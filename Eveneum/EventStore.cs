using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json.Linq;
using Eveneum.Documents;
using System.Threading;
using Eveneum.Advanced;
using Newtonsoft.Json;
using System.Reflection;

namespace Eveneum
{
    public class EventStore : IEventStore, IAdvancedEventStore
    {
        public readonly IDocumentClient Client;
        public readonly string Database;
        public readonly string Collection;
        public readonly string Partition;
        public readonly PartitionKey PartitionKey;

        public DeleteMode DeleteMode { get; set; } = DeleteMode.SoftDelete;

        internal readonly Uri DocumentCollectionUri;

        private readonly JsonSerializer JsonSerializer;
        private readonly JsonSerializerSettings JsonSerializerSettings;
        private readonly TypeCache TypeCache = new TypeCache();

        public EventStore(IDocumentClient client, string database, string collection, string partition = null)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
            this.Database = database ?? throw new ArgumentNullException(nameof(database));
            this.Collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this.Partition = string.IsNullOrEmpty(partition) ? null : partition;
            this.PartitionKey = string.IsNullOrEmpty(this.Partition) ? null : new PartitionKey(this.Partition);

            this.DocumentCollectionUri = UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection);

            this.JsonSerializerSettings = 
                (JsonSerializerSettings)client.GetType().GetField("serializerSettings", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this.Client)
                ?? JsonConvert.DefaultSettings?.Invoke()
                ?? new JsonSerializerSettings();

            this.JsonSerializer = JsonSerializer.Create(this.JsonSerializerSettings);
        }

        private Uri HeaderDocumentUri(string streamId) => UriFactory.CreateDocumentUri(this.Database, this.Collection, HeaderDocument.GenerateId(streamId));

        public async Task<Stream?> ReadStream(string streamId, CancellationToken cancellationToken = default)
        {
            if (streamId == null)
                throw new ArgumentNullException(nameof(streamId));

            var sql = $"SELECT * FROM x WHERE x.{nameof(EveneumDocument.StreamId)} = '{streamId}' ORDER BY x.{nameof(EveneumDocument.SortOrder)} DESC";
            var query = this.Client.CreateDocumentQuery<Document>(this.DocumentCollectionUri, sql, new FeedOptions { PartitionKey = this.PartitionKey }).AsDocumentQuery();

            var documents = new List<EveneumDocument>();
            var finishLoading = false;

            do
            {
                var page = await query.ExecuteNextAsync<Document>(cancellationToken);

                foreach (var document in page)
                {
                    var eveneumDoc = EveneumDocument.Parse(document, this.JsonSerializerSettings);

                    if (eveneumDoc is HeaderDocument && eveneumDoc.Deleted)
                        throw new StreamNotFoundException(streamId);

                    if (eveneumDoc.Deleted)
                        continue;

                    documents.Add(eveneumDoc);

                    if (eveneumDoc is SnapshotDocument)
                    {
                        finishLoading = true;
                        break;
                    }
                }

                if (finishLoading)
                    break;
            }
            while (query.HasMoreResults);

            if (documents.Count == 0)
                return null;

            var headerDocument = documents.First() as HeaderDocument;
            var events = documents.OfType<EventDocument>().Select(this.Deserialize).Reverse().ToArray();
            var snapshot = documents.OfType<SnapshotDocument>().Select(this.Deserialize).Cast<Snapshot?>().FirstOrDefault();

            object metadata = null;

            if (!string.IsNullOrEmpty(headerDocument.MetadataType))
                metadata = headerDocument.Metadata.ToObject(this.TypeCache.Resolve(headerDocument.MetadataType), this.JsonSerializer);

            return new Stream(streamId, headerDocument.Version, metadata, events, snapshot);
        }

        public async Task WriteToStream(string streamId, EventData[] events, ulong? expectedVersion = null, object metadata = null, CancellationToken cancellationToken = default)
        {
            HeaderDocument header;

            // Existing stream
            if (expectedVersion.HasValue)
            {
                header = await this.ReadHeader(streamId, cancellationToken);

                if (header.Version != expectedVersion)
                    throw new OptimisticConcurrencyException(streamId, expectedVersion.Value, header.Version);
            }
            else
            {
                header = new HeaderDocument
                {
                    Partition = this.Partition,
                    StreamId = streamId
                };
            }

            header.Version += (ulong)events.Length;

            if (metadata != null)
            {
                header.Metadata = JToken.FromObject(metadata, this.JsonSerializer);
                header.MetadataType = metadata.GetType().AssemblyQualifiedName;
            }

            if (!expectedVersion.HasValue)
            {
                try
                {
                    await this.Client.CreateDocumentAsync(this.DocumentCollectionUri, header, new RequestOptions { PartitionKey = this.PartitionKey }, disableAutomaticIdGeneration: true, cancellationToken);
                }
                catch (DocumentClientException ex) when (ex.Error.Code == nameof(System.Net.HttpStatusCode.Conflict))
                {
                    throw new StreamAlreadyExistsException(streamId);
                }
            }
            else
            {
                await this.Client.ReplaceDocumentAsync(this.HeaderDocumentUri(streamId), header, new RequestOptions { PartitionKey = this.PartitionKey, AccessCondition = new AccessCondition { Type = AccessConditionType.IfMatch, Condition = header.ETag } }, cancellationToken);
            }

            var eventDocuments = (events ?? Enumerable.Empty<EventData>()).Select(@event => this.Serialize(@event, streamId));

            foreach (var eventDocument in eventDocuments)
                await this.Client.CreateDocumentAsync(this.DocumentCollectionUri, eventDocument, new RequestOptions { PartitionKey = this.PartitionKey }, disableAutomaticIdGeneration: true, cancellationToken);
        }

        public async Task DeleteStream(string streamId, ulong expectedVersion, CancellationToken cancellationToken = default)
        {
            var header = new HeaderDocument
            {
                Partition = this.Partition,
                StreamId = streamId,
                Version = expectedVersion
            };

            var existingHeader = await this.ReadHeader(streamId, cancellationToken);

            if (existingHeader.Deleted)
                throw new StreamNotFoundException(streamId);

            if (existingHeader.Version != expectedVersion)
                throw new OptimisticConcurrencyException(streamId, expectedVersion, existingHeader.Version);

            string etag = existingHeader.ETag;

            var query = this.Client.CreateDocumentQuery<EveneumDocument>(this.DocumentCollectionUri, new FeedOptions { PartitionKey = this.PartitionKey })
                .Where(x => x.StreamId == streamId)
                .AsDocumentQuery();

            while (query.HasMoreResults)
            {
                var page = await query.ExecuteNextAsync<Document>(cancellationToken);

                foreach(var document in page)
                {
                    if (this.DeleteMode == DeleteMode.SoftDelete)
                    {
                        var doc = EveneumDocument.Parse(document, this.JsonSerializerSettings);
                        doc.Deleted = true;
                        await this.Client.UpsertDocumentAsync(this.DocumentCollectionUri, doc, new RequestOptions { PartitionKey = this.PartitionKey }, disableAutomaticIdGeneration: true, cancellationToken);
                    }
                    else
                        await this.Client.DeleteDocumentAsync(document.SelfLink, new RequestOptions { PartitionKey = this.PartitionKey }, cancellationToken);
                }
            }
        }

        public async Task CreateSnapshot(string streamId, ulong version, object snapshot, object metadata = null, bool deleteOlderSnapshots = false, CancellationToken cancellationToken = default)
        {
            var header = await this.ReadHeader(streamId, cancellationToken);

            if (header.Version < version)
                throw new OptimisticConcurrencyException(streamId, version, header.Version);

            var document = this.Serialize(snapshot, metadata, version, streamId);

            await this.Client.UpsertDocumentAsync(this.DocumentCollectionUri, document, new RequestOptions { PartitionKey = this.PartitionKey }, disableAutomaticIdGeneration: true, cancellationToken);

            if (deleteOlderSnapshots)
                await this.DeleteSnapshots(streamId, version, cancellationToken);
        }

        public async Task DeleteSnapshots(string streamId, ulong olderThanVersion, CancellationToken cancellationToken = default)
        {
            await this.ReadHeader(streamId, cancellationToken);

            var query = this.Client.CreateDocumentQuery<SnapshotDocument>(this.DocumentCollectionUri, new FeedOptions { PartitionKey = this.PartitionKey })
                .Where(x => x.StreamId == streamId)
                .Where(x => x.DocumentType == DocumentType.Snapshot)
                .Where(x => x.Version < olderThanVersion)
                .AsDocumentQuery();

            while(query.HasMoreResults)
            {
                var page = await query.ExecuteNextAsync<Document>(cancellationToken);

                foreach(var document in page)
                {
                    if (this.DeleteMode == DeleteMode.SoftDelete)
                    {
                        var doc = EveneumDocument.Parse(document, this.JsonSerializerSettings);
                        doc.Deleted = true;
                        await this.Client.UpsertDocumentAsync(this.DocumentCollectionUri, doc, new RequestOptions { PartitionKey = this.PartitionKey }, disableAutomaticIdGeneration: true, cancellationToken);
                    }
                    else
                        await this.Client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(this.Database, this.Collection, document.Id), new RequestOptions { PartitionKey = this.PartitionKey }, cancellationToken);
                }
            }
        }

        public Task LoadAllEvents(Func<IReadOnlyCollection<EventData>, Task> callback, CancellationToken cancellationToken = default)
        {
            return this.LoadChangeFeed(documents => callback(documents.OfType<EventDocument>().Select(Deserialize).ToList()), cancellationToken: cancellationToken);
        }

        private async Task<HeaderDocument> ReadHeader(string streamId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await this.Client.ReadDocumentAsync<HeaderDocument>(this.HeaderDocumentUri(streamId), new RequestOptions { PartitionKey = this.PartitionKey }, cancellationToken);
            }
            catch (DocumentClientException ex) when (ex.Error.Code == nameof(System.Net.HttpStatusCode.NotFound))
            {
                throw new StreamNotFoundException(streamId);
            }
        }

        private async Task<IReadOnlyCollection<PartitionKeyRange>> GetPartitionKeyRanges()
        {
            string responseContinuation = null;
            var partitionKeyRanges = new List<PartitionKeyRange>();

            do
            {
                var response = await this.Client.ReadPartitionKeyRangeFeedAsync(this.DocumentCollectionUri, new FeedOptions { RequestContinuation = responseContinuation });

                partitionKeyRanges.AddRange(response);
                responseContinuation = response.ResponseContinuation;
            }
            while (responseContinuation != null);

            return partitionKeyRanges;
        }

        private async Task LoadChangeFeed(Func<IEnumerable<EveneumDocument>, Task> callback, string token = null, CancellationToken cancellationToken = default)
        {
            PartitionKeyRange partitionKeyRange = this.PartitionKey != null ? null : (await this.GetPartitionKeyRanges()).FirstOrDefault();

            var changeFeed = this.Client.CreateDocumentChangeFeedQuery(this.DocumentCollectionUri,
                new ChangeFeedOptions
                {
                    PartitionKeyRangeId = partitionKeyRange?.Id,
                    PartitionKey = this.PartitionKey,
                    RequestContinuation = token,
                    StartFromBeginning = true,
                    MaxItemCount = 10000,
                });

            Task callbackTask = null;

            while (changeFeed.HasMoreResults)
            {
                var page = await changeFeed.ExecuteNextAsync<Document>(cancellationToken);

                if (callbackTask != null)
                    await callbackTask;

                callbackTask = callback(page.Select(x => EveneumDocument.Parse(x, this.JsonSerializerSettings)));
            }
        }


        private EventDocument Serialize(EventData @event, string streamId)
        {
            var document = new EventDocument
            {
                Partition = this.Partition,
                StreamId = streamId,
                Version = @event.Version,
                BodyType = @event.Body.GetType().AssemblyQualifiedName,
                Body = JToken.FromObject(@event.Body, this.JsonSerializer)
            };

            if (@event.Metadata != null)
            {
                document.MetadataType = @event.Metadata.GetType().AssemblyQualifiedName;
                document.Metadata = JToken.FromObject(@event.Metadata, this.JsonSerializer);
            }

            return document;
        }

        private SnapshotDocument Serialize(object snapshot, object metadata, ulong version, string streamId)
        {
            var document = new SnapshotDocument
            {
                Partition = this.Partition,
                StreamId = streamId,
                Version = version,
                BodyType = snapshot.GetType().AssemblyQualifiedName,
                Body = JToken.FromObject(snapshot, this.JsonSerializer)
            };

            if (metadata != null)
            {
                document.Metadata = JToken.FromObject(metadata, this.JsonSerializer);
                document.MetadataType = metadata.GetType().AssemblyQualifiedName;
            }

            return document;
        }

        private EventData Deserialize(EventDocument document)
        {
            object metadata = null;

            if (!string.IsNullOrEmpty(document.MetadataType))
                metadata = document.Metadata.ToObject(this.TypeCache.Resolve(document.MetadataType), this.JsonSerializer);

            object body = null;

            if (!string.IsNullOrEmpty(document.BodyType))
                body = document.Body.ToObject(this.TypeCache.Resolve(document.BodyType), this.JsonSerializer);

            return new EventData(body, metadata, document.Version);
        }

        private Snapshot Deserialize(SnapshotDocument document)
        {
            object metadata = null;

            if (!string.IsNullOrEmpty(document.MetadataType))
                metadata = document.Metadata.ToObject(this.TypeCache.Resolve(document.MetadataType), this.JsonSerializer);

            return new Snapshot(document.Body.ToObject(this.TypeCache.Resolve(document.BodyType), this.JsonSerializer), metadata, document.Version);
        }
    }
}
