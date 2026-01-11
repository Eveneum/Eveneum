using Eveneum.Advanced;
using Eveneum.Documents;
using Eveneum.Persistence;
using Eveneum.Serialization;
using Eveneum.Snapshots;
using Eveneum.StoredProcedures;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eveneum
{
    public class EventStore : IEventStore, IAdvancedEventStore
    {
        private readonly ICosmosPersistence Persistence;

        public DeleteMode DeleteMode { get; }
        public TimeSpan StreamTimeToLiveAfterDelete { get; }
        public byte BatchSize { get; }
        public int QueryMaxItemCount { get; }
        public EveneumDocumentSerializer Serializer { get; }
        public ISnapshotWriter SnapshotWriter { get; }
        public SnapshotMode SnapshotMode { get; }

        private const string BulkDeleteStoredProc = "Eveneum.BulkDelete";

        public EventStore(ICosmosPersistence persistence, EventStoreOptions options = null)
        {
            Persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            options = options ?? new EventStoreOptions();

            this.DeleteMode = options.DeleteMode;
            this.StreamTimeToLiveAfterDelete = options.StreamTimeToLiveAfterDelete;
            this.BatchSize = Math.Min(options.BatchSize, (byte)100); // Maximum batch size supported by CosmosDB
            this.QueryMaxItemCount = options.QueryMaxItemCount;
            this.Serializer = new EveneumDocumentSerializer(options.JsonSerializer, options.TypeProvider, options.IgnoreMissingTypes);
            this.SnapshotWriter = options.SnapshotWriter;
            this.SnapshotMode = options.SnapshotMode;
        }

        public async Task Initialize(CancellationToken cancellationToken = default)
        {
            await Persistence.Initialize(cancellationToken);
        }

        public Task<StreamResponse> ReadStream(string streamId, ReadStreamOptions options = null, CancellationToken cancellationToken = default)
        {
            options = options ?? new ReadStreamOptions();

            var maxItemCount = options.MaxItemCount ?? QueryMaxItemCount;

            var whereTerms = new List<string>();

            if (options.IgnoreSnapshots)
                whereTerms.Add($"x.{nameof(EveneumDocument.DocumentType)} <> '{nameof(DocumentType.Snapshot)}'");

            if (options.FromVersion.HasValue)
                whereTerms.Add($"(x.{nameof(EveneumDocument.Version)} >= {options.FromVersion.Value} OR x.{nameof(EveneumDocument.DocumentType)} = '{nameof(DocumentType.Header)}')");

            if (options.ToVersion.HasValue)
                whereTerms.Add($"(x.{nameof(EveneumDocument.Version)} <= {options.ToVersion.Value} OR x.{nameof(EveneumDocument.DocumentType)} = '{nameof(DocumentType.Header)}')");

            var selectClause = "SELECT * FROM x";
            var whereClause = whereTerms.Count > 0 
                ? $"WHERE {string.Join(" AND ", whereTerms)}" 
                : string.Empty;
            var orderByClause = $"ORDER BY x.{nameof(EveneumDocument.SortOrder)} DESC";

            var query = $"{selectClause} {whereClause} {orderByClause}";

            return ReadStream(streamId, query, maxItemCount, cancellationToken);
        }

        private async Task<StreamResponse> ReadStream(string streamId, string sql, int maxItemCount, CancellationToken cancellationToken)
        {
            if (streamId == null)
                throw new ArgumentNullException(nameof(streamId));

            using var iterator = this.Persistence.GetItemQueryIterator(sql, streamId, maxItemCount);

            var documents = new List<IEveneumDocument>();
            var finishLoading = false;
            double requestCharge = 0;

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(cancellationToken);

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

            try
            {
                var events = documents.Where(x => x.DocumentType == DocumentType.Event).Select(this.Serializer.DeserializeEvent).Reverse().ToArray();
                var metadata = this.Serializer.DeserializeObject(headerDocument.MetadataType, headerDocument.Metadata);
                
                var snapshotDocument = documents.FirstOrDefault(x => x.DocumentType == DocumentType.Snapshot);

                Snapshot? snapshot = null;

                if(snapshotDocument is object)
                {
                    snapshot = this.Serializer.DeserializeSnapshot(snapshotDocument);

                    if (snapshot.Value.Data is SnapshotWriterSnapshot)
                        snapshot = await this.SnapshotWriter.ReadSnapshot(streamId, snapshot.Value.Version, cancellationToken);
                }

                return new StreamResponse(new Stream(streamId, headerDocument.Version, metadata, events, snapshot), false, requestCharge);
            }
            catch (TypeNotFoundException ex)
            {
                throw new StreamDeserializationException(streamId, requestCharge, ex.Type, ex);
            }
            catch (JsonDeserializationException ex)
            {
                throw new StreamDeserializationException(streamId, requestCharge, ex.Type, ex);
            }
        }

        public async Task<Response> WriteToStream(string streamId, EventData[] events, ulong? expectedVersion = null, object metadata = null, CancellationToken cancellationToken = default)
        {
            var transaction = Persistence.CreateTransactionalBatch(streamId);
            double requestCharge = 0;

            // Existing stream
            if (expectedVersion.HasValue)
            {
                var headerResponse = await this.ReadHeaderDocument(streamId, cancellationToken);

                var header = headerResponse.Document;
                requestCharge += headerResponse.RequestCharge;

                if (header.Deleted)
                    throw new StreamDeletedException(streamId, requestCharge);

                if (header.Version != expectedVersion)
                    throw new OptimisticConcurrencyException(streamId, requestCharge, expectedVersion.Value, header.Version);

                header.Version += (ulong)events.Length;

                this.Serializer.SerializeHeaderMetadata(header, metadata);

                transaction.ReplaceItem(header.Id, header, new TransactionalBatchItemRequestOptions { IfMatchEtag = header.ETag });
            }
            else
            {
                var header = this.Serializer.JsonSerializer.CreateDocument(streamId, DocumentType.Header);
                header.StreamId = streamId;
                header.Version = (ulong)events.Length;

                this.Serializer.SerializeHeaderMetadata(header, metadata);

                transaction.CreateItem(header);
            }

            var firstBatch = events.Take(this.BatchSize - 1).Select(@event => this.Serializer.SerializeEvent(@event, streamId));
            foreach (var document in firstBatch)
                transaction.CreateItem(document);

            using var response = await transaction.ExecuteAsync(cancellationToken);
            requestCharge += response.RequestCharge;

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                if (response.GetOperationResultAtIndex<EveneumDocument>(0).StatusCode == System.Net.HttpStatusCode.Conflict)
                    throw new StreamAlreadyExistsException(streamId, requestCharge);
                else
                {
                    foreach (var index in Enumerable.Range(1, events.Length))
                    {
                        if (response.GetOperationResultAtIndex<EveneumDocument>(index).StatusCode == System.Net.HttpStatusCode.Conflict)
                            throw new EventAlreadyExistsException(streamId, events[index - 1].Version, requestCharge);
                    }
                }
            }
            else if (!response.IsSuccessStatusCode)
                throw new WriteException(streamId, requestCharge, response.ErrorMessage, response.StatusCode);

            foreach (var batch in events.Skip(this.BatchSize - 1).Select(@event => this.Serializer.SerializeEvent(@event, streamId)).Batch(this.BatchSize))
            {
                if(!batch.Any())
                    continue;

                transaction = Persistence.CreateTransactionalBatch(streamId);

                foreach (var document in batch)
                    transaction.CreateItem(document);

                using var batchResponse = await transaction.ExecuteAsync(cancellationToken);
                requestCharge += batchResponse.RequestCharge;

                if (batchResponse.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    foreach (var index in Enumerable.Range(0, batch.Count()))
                    {
                        if (batchResponse.GetOperationResultAtIndex<EveneumDocument>(index).StatusCode == System.Net.HttpStatusCode.Conflict)
                            throw new EventAlreadyExistsException(streamId, batch.ElementAt(index).Version, requestCharge);
                    }
                }
                else if(!batchResponse.IsSuccessStatusCode)
                    throw new WriteException(streamId, requestCharge, batchResponse.ErrorMessage, batchResponse.StatusCode);
            }

            return new Response(requestCharge);
        }

        public async Task<DeleteResponse> DeleteStream(string streamId, ulong expectedVersion, CancellationToken cancellationToken = default)
        {
            var headerResponse = await this.ReadHeaderDocument(streamId, cancellationToken);

            var existingHeader = headerResponse.Document;
            var requestCharge = headerResponse.RequestCharge;

            if (existingHeader == null)
                throw new StreamNotFoundException(streamId, requestCharge);

            if (existingHeader.Deleted)
                throw new StreamDeletedException(streamId, requestCharge);

            if (existingHeader.Version != expectedVersion)
                throw new OptimisticConcurrencyException(streamId, requestCharge, expectedVersion, existingHeader.Version);

            ulong deletedDocuments = 0;

            StoredProcedureExecuteResponse<BulkDeleteResponse> response;
            var query = $"SELECT * FROM c";

            var useSoftDeleteMode = (this.DeleteMode == DeleteMode.SoftDelete) || (this.DeleteMode == DeleteMode.TtlDelete);

            if (useSoftDeleteMode)
                query += $" WHERE c.{nameof(EveneumDocument.Deleted)} = false";

            do
            {
                var ttl = this.DeleteMode == DeleteMode.TtlDelete ? StreamTimeToLiveAfterDelete.TotalSeconds  : -1;                
                response = await Persistence.ExecuteStoredProcedureAsync<BulkDeleteResponse>(BulkDeleteStoredProc, streamId, new object[] { query, useSoftDeleteMode, ttl }, cancellationToken);

                requestCharge += response.RequestCharge;
                deletedDocuments += response.Resource.Deleted;
            }
            while (response.Resource.Continuation);

            return new DeleteResponse(deletedDocuments, requestCharge);
        }

        public async Task<Response> CreateSnapshot(string streamId, ulong version, object snapshot, object metadata = null, bool deleteOlderSnapshots = false, CancellationToken cancellationToken = default)
        {
            var headerResponse = await this.ReadHeaderDocument(streamId, cancellationToken);

            var header = headerResponse.Document;
            var requestCharge = headerResponse.RequestCharge;

            if (header == null)
                throw new StreamNotFoundException(streamId, requestCharge);

            if (header.Deleted)
                throw new StreamDeletedException(streamId, requestCharge);

            if (header.Version < version)
                throw new OptimisticConcurrencyException(streamId, requestCharge, version, header.Version);

            var customSnapshotCreated = false;

            if(this.SnapshotWriter is object)
                customSnapshotCreated = await this.SnapshotWriter.CreateSnapshot(streamId, version, snapshot, metadata, cancellationToken);

            var document = customSnapshotCreated
                ? this.Serializer.SerializeSnapshot(new SnapshotWriterSnapshot(this.SnapshotWriter.GetType().AssemblyQualifiedName), null, version, streamId, this.SnapshotMode)
                : this.Serializer.SerializeSnapshot(snapshot, metadata, version, streamId, this.SnapshotMode);

            var response = await Persistence.UpsertItemAsync(document, streamId, cancellationToken);

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
            var query = $"SELECT * FROM c WHERE c.{nameof(EveneumDocument.DocumentType)} = 'Snapshot' AND c.Version < {olderThanVersion}";

            return await DeleteDocuments(streamId, query, cancellationToken);
        }

        public Task<Response> LoadAllEvents(Func<IReadOnlyCollection<EventData>, Task> callback, CancellationToken cancellationToken = default) =>
            this.LoadEvents($"SELECT * FROM c WHERE c.{nameof(EveneumDocument.DocumentType)} = '{nameof(DocumentType.Event)}'", callback, cancellationToken);

        public Task<Response> LoadEvents(string query, Func<IReadOnlyCollection<EventData>, Task> callback, CancellationToken cancellationToken = default)
            => this.LoadEvents(new QueryDefinition(query), callback, cancellationToken);

        public Task<Response> LoadEvents(QueryDefinition query, Func<IReadOnlyCollection<EventData>, Task> callback, CancellationToken cancellationToken = default)
            => LoadDocuments(query, response => callback(response.Where(x => x.DocumentType == DocumentType.Event).Select(this.Serializer.DeserializeEvent).ToList()), cancellationToken);

        public Task<Response> LoadStreamHeaders(string query, Func<IReadOnlyCollection<StreamHeader>, Task> callback, CancellationToken cancellationToken = default)
            => this.LoadStreamHeaders(new QueryDefinition(query), callback, cancellationToken);

        public Task<Response> LoadStreamHeaders(QueryDefinition query, Func<IReadOnlyCollection<StreamHeader>, Task> callback, CancellationToken cancellationToken = default)
            => LoadDocuments(query, response => callback(response.Where(x => x.DocumentType == DocumentType.Header).Select(x => new StreamHeader(x.StreamId, x.Version, this.Serializer.DeserializeObject(x.MetadataType, x.Metadata), x.Deleted)).ToList()), cancellationToken);

        public async Task<Response> ReplaceEvent(EventData newEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await Persistence.ReplaceItemAsync(
                    this.Serializer.SerializeEvent(newEvent, newEvent.StreamId), 
                    EveneumDocumentSerializer.GenerateEventId(newEvent.StreamId, newEvent.Version), 
                    newEvent.StreamId, 
                    cancellationToken);

                return new Response(response.RequestCharge);
            }
            catch (CosmosException ex)
            {
                throw new WriteException(newEvent.StreamId, ex.RequestCharge, ex.Message, ex.StatusCode, ex);
            }
        }

        public async Task<DeleteResponse> DeleteEvent(string streamId, ulong version, CancellationToken cancellationToken = default)
        {
            var query = $"SELECT * FROM c WHERE c.{nameof(EveneumDocument.DocumentType)} = 'Event' AND c.Version = {version}";

            return await DeleteDocuments(streamId, query, cancellationToken);
        }

        public async Task<StreamHeaderResponse> ReadHeader(string streamId, CancellationToken cancellationToken = default)
        {
            var result = await this.ReadHeaderDocument(streamId, cancellationToken);

            return new StreamHeaderResponse(new StreamHeader(streamId, result.Document.Version, this.Serializer.DeserializeObject(result.Document.MetadataType, result.Document.Metadata), result.Document.Deleted), result.RequestCharge);
        }

        private async Task<Response> LoadDocuments(QueryDefinition query, Func<IEnumerable<IEveneumDocument>, Task> callback, CancellationToken cancellationToken = default)
        {
            using var iterator = this.Persistence.GetItemQueryIterator(query, this.QueryMaxItemCount);

            double requestCharge = 0;
            var callbackProcessing = Task.CompletedTask;

            do
            {
                var response = await iterator.ReadNextAsync(cancellationToken);

                requestCharge += response.RequestCharge;

                await callbackProcessing;

                callbackProcessing = callback(response);
            }
            while (iterator.HasMoreResults);

            await callbackProcessing;

            return new Response(requestCharge);
        }

        private async Task<DocumentResponse> ReadHeaderDocument(string streamId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await this.Persistence.ReadItemAsync(streamId, streamId, cancellationToken);

                return new DocumentResponse(result.Resource, result.RequestCharge);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new StreamNotFoundException(streamId, ex.RequestCharge, ex);
            }
        }

        private async Task<DeleteResponse> DeleteDocuments(string streamId, string query, CancellationToken cancellationToken)
        {
            var headerResponse = await this.ReadHeader(streamId, cancellationToken);

            var requestCharge = headerResponse.RequestCharge;
            ulong deletedSnapshots = 0;
            StoredProcedureExecuteResponse<BulkDeleteResponse> response;
            if (this.DeleteMode == DeleteMode.SoftDelete)
                query += $" AND c.{nameof(EveneumDocument.Deleted)} = false";

            do
            {
                response = await this.Persistence.ExecuteStoredProcedureAsync<BulkDeleteResponse>(BulkDeleteStoredProc, streamId, new object[] { query, this.DeleteMode == DeleteMode.SoftDelete }, cancellationToken);
                requestCharge += response.RequestCharge;
                deletedSnapshots += response.Resource.Deleted;
            }
            while (response.Resource.Continuation);

            return new DeleteResponse(deletedSnapshots, requestCharge);
        }
    }
}
