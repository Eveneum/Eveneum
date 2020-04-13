using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using Eveneum.Advanced;
using Eveneum.Documents;
using Eveneum.Serialization;
using Eveneum.StoredProcedures;

namespace Eveneum
{
    public class EventStore : IEventStore, IAdvancedEventStore
    {
        public readonly CosmosClient Client;
        public readonly Database Database;
        public readonly Container Container;

        public DeleteMode DeleteMode { get; }
        public byte BatchSize { get; }
        public EveneumDocumentSerializer Serializer { get; }
        
        private const string BulkDeleteStoredProc = "Eveneum.BulkDelete";

        public EventStore(CosmosClient client, string database, string container, EventStoreOptions options = null)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client)); 
            this.Database = this.Client.GetDatabase(database ?? throw new ArgumentNullException(nameof(database)));
            this.Container = this.Database.GetContainer(container ?? throw new ArgumentNullException(nameof(container)));

            options = options ?? new EventStoreOptions();

            this.DeleteMode = options.DeleteMode;
            this.BatchSize = Math.Min(options.BatchSize, (byte)100); // Maximum batch size supported by CosmosDB
            this.Serializer = new EveneumDocumentSerializer(options.JsonSerializer, options.TypeProvider, options.IgnoreMissingTypes);
        }

        public async Task Initialize()
        {
            await CreateStoredProcedure(BulkDeleteStoredProc, "BulkDelete");
        }

        public Task<StreamResponse> ReadStream(string streamId, CancellationToken cancellationToken = default) =>
            this.ReadStream(streamId, $"SELECT * FROM x ORDER BY x.{nameof(EveneumDocument.SortOrder)} DESC", 100, cancellationToken);

        public Task<StreamResponse> ReadStreamAsOfVersion(string streamId, ulong version, CancellationToken cancellationToken = default) =>
            this.ReadStream(streamId, $"SELECT * FROM x WHERE x.{nameof(EveneumDocument.Version)} <= {version} OR x.{nameof(EveneumDocument.DocumentType)} = '{nameof(DocumentType.Header)}' ORDER BY x.{nameof(EveneumDocument.SortOrder)} DESC", 100, cancellationToken);

        public Task<StreamResponse> ReadStreamFromVersion(string streamId, ulong version, CancellationToken cancellationToken = default) =>
            this.ReadStream(streamId, $"SELECT * FROM x WHERE (x.{nameof(EveneumDocument.Version)} >= {version} AND x.{nameof(EveneumDocument.DocumentType)} <> '{nameof(DocumentType.Snapshot)}') OR x.{nameof(EveneumDocument.DocumentType)} = '{nameof(DocumentType.Header)}' ORDER BY x.{nameof(EveneumDocument.SortOrder)} DESC", -1, cancellationToken);

        public Task<StreamResponse> ReadStreamIgnoringSnapshots(string streamId, CancellationToken cancellationToken = default) =>
            this.ReadStream(streamId, $"SELECT * FROM x WHERE x.{nameof(EveneumDocument.DocumentType)} <> '{nameof(DocumentType.Snapshot)}' ORDER BY x.{nameof(EveneumDocument.SortOrder)} DESC", -1, cancellationToken);

        private async Task<StreamResponse> ReadStream(string streamId, string sql, int maxItemCount, CancellationToken cancellationToken)
        {
            if (streamId == null)
                throw new ArgumentNullException(nameof(streamId));

            var query = this.Container.GetItemQueryIterator<EveneumDocument>(sql, requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(streamId), MaxItemCount = maxItemCount });

            var documents = new List<EveneumDocument>();
            var finishLoading = false;
            double requestCharge = 0;

            while (query.HasMoreResults)
            {
                var page = await query.ReadNextAsync(cancellationToken);
                requestCharge += page.RequestCharge;

                foreach (var eveneumDoc in page)
                {
                    if (eveneumDoc.DocumentType == DocumentType.Header && eveneumDoc.Deleted)
                        return new StreamResponse(null, true, requestCharge);

                    if (eveneumDoc.Deleted)
                        continue;

                    documents.Add(eveneumDoc);

                    if (eveneumDoc.DocumentType == DocumentType.Snapshot)
                    {
                        finishLoading = true;
                        break;
                    }
                }

                if (finishLoading)
                    break;
            }

            if (documents.Count == 0)
                return new StreamResponse(null, false, requestCharge);

            var headerDocument = documents.First(x => x.DocumentType == DocumentType.Header);
            var events = documents.Where(x => x.DocumentType == DocumentType.Event).Select(this.Serializer.DeserializeEvent).Reverse().ToArray();
            var snapshot = documents.Where(x => x.DocumentType == DocumentType.Snapshot).Select(this.Serializer.DeserializeSnapshot).Cast<Snapshot?>().FirstOrDefault();
            var metadata = this.Serializer.DeserializeObject(headerDocument.MetadataType, headerDocument.Metadata);

            return new StreamResponse(new Stream(streamId, headerDocument.Version, metadata, events, snapshot), false, requestCharge);
        }

        public async Task<Response> WriteToStream(string streamId, EventData[] events, ulong? expectedVersion = null, object metadata = null, CancellationToken cancellationToken = default)
        {
            var transaction = this.Container.CreateTransactionalBatch(new PartitionKey(streamId));
            double requestCharge = 0;

            // Existing stream
            if (expectedVersion.HasValue)
            {
                var headerResponse = await this.ReadHeader(streamId, cancellationToken);

                var header = headerResponse.Document;
                requestCharge += headerResponse.RequestCharge;

                if (header.Deleted)
                    throw new StreamDeletedException(streamId);

                if (header.Version != expectedVersion)
                    throw new OptimisticConcurrencyException(streamId, expectedVersion.Value, header.Version);

                header.Version += (ulong)events.Length;

                this.Serializer.SerializeHeaderMetadata(header, metadata);
                
                transaction.ReplaceItem(header.Id, header, new TransactionalBatchItemRequestOptions { IfMatchEtag = header.ETag });
            }
            else
            {
                var header = new EveneumDocument(DocumentType.Header) { StreamId = streamId, Version = (ulong)events.Length };

                this.Serializer.SerializeHeaderMetadata(header, metadata);

                transaction.CreateItem(header);
            }

            var firstBatch = events.Take(this.BatchSize - 1).Select(@event => this.Serializer.SerializeEvent(@event, streamId));
            foreach (var document in firstBatch)
                transaction.CreateItem(document);

            var response = await transaction.ExecuteAsync(cancellationToken);
            requestCharge += response.RequestCharge;

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                throw new StreamAlreadyExistsException(streamId);
            else
                if (!response.IsSuccessStatusCode)
                    throw new WriteException(response.ErrorMessage, response.StatusCode);

            foreach (var batch in events.Skip(this.BatchSize - 1).Select(@event => this.Serializer.SerializeEvent(@event, streamId)).Batch(this.BatchSize))
            {
                transaction = this.Container.CreateTransactionalBatch(new PartitionKey(streamId));
                
                foreach (var document in batch)
                    transaction.CreateItem(document);

                response = await transaction.ExecuteAsync(cancellationToken);
                requestCharge += response.RequestCharge;

                if (!response.IsSuccessStatusCode)
                    throw new WriteException(response.ErrorMessage, response.StatusCode);
            }

            return new Response(requestCharge);
        }

        public async Task<DeleteResponse> DeleteStream(string streamId, ulong expectedVersion, CancellationToken cancellationToken = default)
        {
            var headerResponse = await this.ReadHeader(streamId, cancellationToken);

            var existingHeader = headerResponse.Document;
            var requestCharge = headerResponse.RequestCharge;

            if (existingHeader == null)
                throw new StreamNotFoundException(streamId);

            if (existingHeader.Deleted)
                throw new StreamDeletedException(streamId);

            if (existingHeader.Version != expectedVersion)
                throw new OptimisticConcurrencyException(streamId, expectedVersion, existingHeader.Version);

            var partitionKey = new PartitionKey(streamId);
            ulong deletedDocuments = 0;

            StoredProcedureExecuteResponse<BulkDeleteResponse> response;
            var query = $"SELECT * FROM c";

            if (this.DeleteMode == DeleteMode.SoftDelete)
                query += " WHERE c.Deleted = false";

            do
            {
                response = await this.Container.Scripts.ExecuteStoredProcedureAsync<BulkDeleteResponse>(BulkDeleteStoredProc, partitionKey, new object[] { query, this.DeleteMode == DeleteMode.SoftDelete }, cancellationToken: cancellationToken);
                requestCharge += response.RequestCharge;
                deletedDocuments += response.Resource.Deleted;
            }
            while (response.Resource.Continuation);
            
            return new DeleteResponse(deletedDocuments, requestCharge);
        }

        public async Task<Response> CreateSnapshot(string streamId, ulong version, object snapshot, object metadata = null, bool deleteOlderSnapshots = false, CancellationToken cancellationToken = default)
        {
            var headerResponse = await this.ReadHeader(streamId, cancellationToken);

            var header = headerResponse.Document;
            var requestCharge = headerResponse.RequestCharge;

            if (header == null)
                throw new StreamNotFoundException(streamId);

            if (header.Deleted)
                throw new StreamDeletedException(streamId);

            if (header.Version < version)
                throw new OptimisticConcurrencyException(streamId, version, header.Version);

            var document = this.Serializer.SerializeSnapshot(snapshot, metadata, version, streamId);

            var response = await this.Container.UpsertItemAsync(document, new PartitionKey(streamId), cancellationToken: cancellationToken);

            requestCharge += response.RequestCharge;

            if (deleteOlderSnapshots)
            {
                var deleteResponse = await this.DeleteSnapshots(streamId, version, cancellationToken);

                requestCharge += deleteResponse.RequestCharge;
            }

            return new Response(requestCharge);
        }

        public async Task<DeleteResponse> DeleteSnapshots(string streamId, ulong olderThanVersion, CancellationToken cancellationToken = default)
        {
            var headerResponse = await this.ReadHeader(streamId, cancellationToken);

            var requestCharge = headerResponse.RequestCharge;
            ulong deletedSnapshots = 0;
            StoredProcedureExecuteResponse<BulkDeleteResponse> response;
            var query = $"SELECT * FROM c WHERE c.DocumentType = 'Snapshot' AND c.Version < {olderThanVersion}";

            if (this.DeleteMode == DeleteMode.SoftDelete)
                query += " and c.Deleted = false";

            do
            {
                response = await this.Container.Scripts.ExecuteStoredProcedureAsync<BulkDeleteResponse>(BulkDeleteStoredProc, new PartitionKey(streamId), new object[] { query, this.DeleteMode == DeleteMode.SoftDelete }, cancellationToken: cancellationToken);
                requestCharge += response.RequestCharge;
                deletedSnapshots += response.Resource.Deleted;
            }
            while (response.Resource.Continuation);

            return new DeleteResponse(deletedSnapshots, requestCharge);
        }

        public Task<Response> LoadAllEvents(Func<IReadOnlyCollection<EventData>, Task> callback, CancellationToken cancellationToken = default) =>
            this.LoadEvents($"SELECT * FROM c WHERE c.{nameof(EveneumDocument.DocumentType)} = '{nameof(DocumentType.Event)}'", callback, cancellationToken);

        public async Task<Response> LoadEvents(string sql, Func<IReadOnlyCollection<EventData>, Task> callback, CancellationToken cancellationToken = default)
        {
            double requestCharge = 0;
            var query = this.Container.GetItemQueryIterator<EveneumDocument>(sql, requestOptions: new QueryRequestOptions { MaxItemCount = -1 });

            do
            {
                var page = await query.ReadNextAsync(cancellationToken);

                requestCharge += page.RequestCharge;

                await callback(page.Where(x => x.DocumentType == DocumentType.Event).Where(x => !x.Deleted).Select(this.Serializer.DeserializeEvent).ToList());
            }
            while (query.HasMoreResults);

            return new Response(requestCharge);
        }

        public async Task<Response> LoadStreamHeaders(string sql, Func<IReadOnlyCollection<StreamHeader>, Task> callback, CancellationToken cancellationToken = default)
        {
            double requestCharge = 0;
            var query = this.Container.GetItemQueryIterator<EveneumDocument>(sql, requestOptions: new QueryRequestOptions { MaxItemCount = -1 });

            do
            {
                var page = await query.ReadNextAsync(cancellationToken);

                requestCharge += page.RequestCharge;

                await callback(page.Where(x => x.DocumentType == DocumentType.Header).Where(x => !x.Deleted).Select(x => new StreamHeader(x.StreamId, x.Version, this.Serializer.DeserializeObject(x.MetadataType, x.Metadata))).ToList());
            }
            while (query.HasMoreResults);

            return new Response(requestCharge);
        }

        public async Task<Response> ReplaceEvent(EventData newEvent, CancellationToken cancellationToken = default)
        {
            var response = await this.Container.ReplaceItemAsync(this.Serializer.SerializeEvent(newEvent, newEvent.StreamId), EveneumDocument.GenerateEventId(newEvent.StreamId, newEvent.Version), new PartitionKey(newEvent.StreamId), cancellationToken: cancellationToken);
            
            return new Response(response.RequestCharge);
        }

        private async Task<DocumentResponse> ReadHeader(string streamId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await this.Container.ReadItemAsync<EveneumDocument>(streamId, new PartitionKey(streamId), cancellationToken: cancellationToken);

                return new DocumentResponse(result.Resource, result.RequestCharge);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new StreamNotFoundException(streamId);
            }
        }

        private async Task CreateStoredProcedure(string procedureId, string procedureFileName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(EventStore), $"StoredProcedures.{procedureFileName}.js"))
            using (var reader = new StreamReader(stream))
            {
                var properties = new StoredProcedureProperties
                {
                    Id = procedureId,
                    Body = await reader.ReadToEndAsync()
                };

                try
                {
                    await this.Container.Scripts.ReadStoredProcedureAsync(procedureId);
                    await this.Container.Scripts.ReplaceStoredProcedureAsync(properties);
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await this.Container.Scripts.CreateStoredProcedureAsync(properties);
                }
            }
        }
    }
}
