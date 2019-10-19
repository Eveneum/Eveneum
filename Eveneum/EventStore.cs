using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Cosmos.Scripts;
using Newtonsoft.Json.Linq;
using Eveneum.Advanced;
using Eveneum.Documents;
using Eveneum.Serialization;

namespace Eveneum
{
    public class EventStore : IEventStore, IAdvancedEventStore
    {
        public readonly CosmosClient Client;
        public readonly Database Database;
        public readonly Container Container;

        public DeleteMode DeleteMode { get; }
        public EveneumDocumentSerializer Serializer { get; }
        
        private bool IsInitialized = false;
        private const string WriteEventsStoredProc = "Eveneum.WriteEvents";

        public EventStore(CosmosClient client, string database, string container, EventStoreOptions options = null)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client)); 
            this.Database = this.Client.GetDatabase(database ?? throw new ArgumentNullException(nameof(database)));
            this.Container = this.Database.GetContainer(container ?? throw new ArgumentNullException(nameof(container)));

            this.DeleteMode = options?.DeleteMode ?? DeleteMode.SoftDelete;

            this.Serializer = new EveneumDocumentSerializer(options?.JsonSerializer, options?.TypeProvider);
        }

        public async Task Initialize()
        {
            await CreateStoredProcedure(WriteEventsStoredProc, "WriteEvents");

            this.IsInitialized = true;
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
            if (!this.IsInitialized)
                throw new NotInitializedException();

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
                        throw new StreamNotFoundException(streamId);

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
                return new StreamResponse(null, requestCharge);

            var headerDocument = documents.First(x => x.DocumentType == DocumentType.Header);
            var events = documents.Where(x => x.DocumentType == DocumentType.Event).Select(this.Serializer.DeserializeEvent).Reverse().ToArray();
            var snapshot = documents.Where(x => x.DocumentType == DocumentType.Snapshot).Select(this.Serializer.DeserializeSnapshot).Cast<Snapshot?>().FirstOrDefault();
            var metadata = this.Serializer.DeserializeObject(headerDocument.MetadataType, headerDocument.Metadata);

            return new StreamResponse(new Stream(streamId, headerDocument.Version, metadata, events, snapshot), requestCharge);
        }

        public async Task<Response> WriteToStream(string streamId, EventData[] events, ulong? expectedVersion = null, object metadata = null, CancellationToken cancellationToken = default)
        {
            if (!this.IsInitialized)
                throw new NotInitializedException();

            EveneumDocument header;
            double requestCharge = 0;

            // Existing stream
            if (expectedVersion.HasValue)
            {
                var headerResponse = await this.ReadHeader(streamId, cancellationToken);

                header = headerResponse.Document;
                requestCharge += headerResponse.RequestCharge;

                if (header.Version != expectedVersion)
                    throw new OptimisticConcurrencyException(streamId, expectedVersion.Value, header.Version);
            }
            else
                header = new EveneumDocument(DocumentType.Header) { StreamId = streamId };

            header.Version += (ulong)events.Length;

            this.Serializer.SerializeHeaderMetadata(header, metadata);

            if (!expectedVersion.HasValue)
            {
                try
                {
                    var response = await this.Container.CreateItemAsync(header, new PartitionKey(streamId), cancellationToken: cancellationToken);

                    requestCharge += response.RequestCharge;
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    throw new StreamAlreadyExistsException(streamId);
                }
            }
            else
            {
                var response = await this.Container.ReplaceItemAsync(header, header.Id, new PartitionKey(streamId), new ItemRequestOptions { IfMatchEtag = header.ETag }, cancellationToken);

                requestCharge += response.RequestCharge;
            }

            var eventDocuments = (events ?? Enumerable.Empty<EventData>()).Select(@event => this.Serializer.SerializeEvent(@event, streamId)).ToList();

            while(eventDocuments.Count > 0)
            {
                var response = await this.Container.Scripts.ExecuteStoredProcedureAsync<int>(WriteEventsStoredProc, new PartitionKey(streamId), new[] { eventDocuments }, cancellationToken: cancellationToken);
                requestCharge += response.RequestCharge;

                eventDocuments.RemoveRange(0, response.Resource);
            }

            return new Response(requestCharge);
        }

        public async Task<Response> DeleteStream(string streamId, ulong expectedVersion, CancellationToken cancellationToken = default)
        {
            if (!this.IsInitialized)
                throw new NotInitializedException();

            var headerResponse = await this.ReadHeader(streamId, cancellationToken);

            var existingHeader = headerResponse.Document;
            var requestCharge = headerResponse.RequestCharge;

            if (existingHeader.Deleted)
                throw new StreamNotFoundException(streamId);

            if (existingHeader.Version != expectedVersion)
                throw new OptimisticConcurrencyException(streamId, expectedVersion, existingHeader.Version);

            var partitionKey = new PartitionKey(streamId);

            var query = this.Container.GetItemQueryIterator<EveneumDocument>(new QueryDefinition("SELECT * FROM x"), requestOptions: new QueryRequestOptions { PartitionKey = partitionKey, MaxItemCount = -1 });

            do
            {
                var page = await query.ReadNextAsync(cancellationToken);

                requestCharge += page.RequestCharge;

                foreach (var document in page)
                {
                    if (this.DeleteMode == DeleteMode.SoftDelete)
                    {
                        document.Deleted = true;

                        var response = await this.Container.UpsertItemAsync(document, partitionKey, cancellationToken: cancellationToken);

                        requestCharge += response.RequestCharge;
                    }
                    else
                    { 
                        var response = await this.Container.DeleteItemAsync<EveneumDocument>(document.Id, partitionKey, cancellationToken: cancellationToken);

                        requestCharge += response.RequestCharge;
                    }
                }
            } while (query.HasMoreResults);

            return new Response(requestCharge);
        }

        public async Task<Response> CreateSnapshot(string streamId, ulong version, object snapshot, object metadata = null, bool deleteOlderSnapshots = false, CancellationToken cancellationToken = default)
        {
            if (!this.IsInitialized)
                throw new NotInitializedException();

            var headerResponse = await this.ReadHeader(streamId, cancellationToken);

            var header = headerResponse.Document;
            var requestCharge = headerResponse.RequestCharge;

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

        public async Task<Response> DeleteSnapshots(string streamId, ulong olderThanVersion, CancellationToken cancellationToken = default)
        {
            if (!this.IsInitialized)
                throw new NotInitializedException();

            var headerResponse = await this.ReadHeader(streamId, cancellationToken);

            var requestCharge = headerResponse.RequestCharge;

            var partitionKey = new PartitionKey(streamId);

            var query = this.Container.GetItemLinqQueryable<EveneumDocument>(requestOptions: new QueryRequestOptions { PartitionKey = partitionKey, MaxItemCount = -1 })
                .Where(x => x.DocumentType == DocumentType.Snapshot)
                .Where(x => x.Version < olderThanVersion)
                .ToFeedIterator();

            do
            {
                var page = await query.ReadNextAsync(cancellationToken);

                requestCharge += page.RequestCharge;

                foreach (var document in page)
                {
                    if (this.DeleteMode == DeleteMode.SoftDelete)
                    {
                        document.Deleted = true;

                        var response = await this.Container.UpsertItemAsync(document, partitionKey, cancellationToken: cancellationToken);

                        requestCharge += response.RequestCharge;
                    }
                    else
                    {
                        var response = await this.Container.DeleteItemAsync<EveneumDocument>(document.Id, partitionKey, cancellationToken: cancellationToken);

                        requestCharge += response.RequestCharge;
                    }
                }
            } while (query.HasMoreResults);

            return new Response(requestCharge);
        }

        public Task<Response> LoadAllEvents(Func<IReadOnlyCollection<EventData>, Task> callback, CancellationToken cancellationToken = default) =>
            this.LoadEvents($"SELECT * FROM c WHERE c.{nameof(EveneumDocument.DocumentType)} = '{nameof(DocumentType.Event)}'", callback, cancellationToken);

        public async Task<Response> LoadEvents(string sql, Func<IReadOnlyCollection<EventData>, Task> callback, CancellationToken cancellationToken = default)
        {
            if (!this.IsInitialized)
                throw new NotInitializedException();

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
