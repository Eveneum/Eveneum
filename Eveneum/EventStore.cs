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
using System.Collections.Concurrent;

namespace Eveneum
{
    public class EventStore : IEventStore
    {
        public readonly DocumentClient Client;
        public readonly string Database;
        public readonly string Collection;
        public readonly string Partition;
        public readonly PartitionKey PartitionKey;

        private readonly Uri DocumentCollectionUri;

        private readonly TypeCache TypeCache = new TypeCache();

        public EventStore(DocumentClient client, string database, string collection, string partition = null)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
            this.Database = database ?? throw new ArgumentNullException(nameof(database));
            this.Collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this.Partition = string.IsNullOrEmpty(partition) ? null : partition;
            this.PartitionKey = string.IsNullOrEmpty(this.Partition) ? null : new PartitionKey(this.Partition);

            this.DocumentCollectionUri = UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection);
        }

        private Uri HeaderDocumentUri(string streamId) => UriFactory.CreateDocumentUri(this.Database, this.Collection, HeaderDocument.GenerateId(streamId));

        public async Task<Stream?> ReadStream(string streamId, CancellationToken cancellationToken = default)
        {
            if (streamId == null)
                throw new ArgumentNullException(nameof(streamId));

            var sql = $"SELECT * FROM x WHERE x.{nameof(EveneumDocument.StreamId)} = '{streamId}' ORDER BY x.{nameof(EveneumDocument.SortOrder)} DESC";
            var query = this.Client.CreateDocumentQuery<Document>(this.DocumentCollectionUri, sql, new FeedOptions { PartitionKey = this.PartitionKey }).AsDocumentQuery();

            var page = await query.ExecuteNextAsync<Document>(cancellationToken);

            if (page.Count == 0)
                return null;

            var documents = new List<EveneumDocument>();
            var finishLoading = false;

            do
            {
                foreach (var document in page)
                {
                    var eveneumDoc = EveneumDocument.Parse(document);

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

                page = await query.ExecuteNextAsync<Document>(cancellationToken);
            }
            while (query.HasMoreResults);

            var headerDocument = documents.First() as HeaderDocument;
            var events = documents.OfType<EventDocument>().Select(this.Deserialize).Reverse().ToArray();
            var snapshot = documents.OfType<SnapshotDocument>().Select(this.Deserialize).Cast<Snapshot?>().FirstOrDefault();

            object metadata = null;

            if (!string.IsNullOrEmpty(headerDocument.MetadataType))
                metadata = headerDocument.Metadata.ToObject(this.TypeCache.Resolve(headerDocument.MetadataType));

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
                header.Metadata = JToken.FromObject(metadata);
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
                .Where(x => !x.Deleted)
                .AsDocumentQuery();

            while (query.HasMoreResults)
            {
                var page = await query.ExecuteNextAsync<Document>(cancellationToken);

                foreach (var document in page)
                {
                    var doc = EveneumDocument.Parse(document);
                    doc.Deleted = true;

                    await this.Client.UpsertDocumentAsync(this.DocumentCollectionUri, doc, new RequestOptions { PartitionKey = this.PartitionKey }, disableAutomaticIdGeneration: true, cancellationToken);
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

            var documents = new List<SnapshotDocument>();

            while(query.HasMoreResults)
            {
                var page = await query.ExecuteNextAsync<SnapshotDocument>(cancellationToken);
                documents.AddRange(page);
            }

            var tasks = documents.Select(async document =>
            {
                document.Deleted = true;
                
                await this.Client.UpsertDocumentAsync(this.DocumentCollectionUri, document,new RequestOptions { PartitionKey = this.PartitionKey }, disableAutomaticIdGeneration: true, cancellationToken);
            });

            await Task.WhenAll(tasks);
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

        private EventDocument Serialize(EventData @event, string streamId)
        {
            var document = new EventDocument
            {
                Partition = this.Partition,
                StreamId = streamId,
                Version = @event.Version,
                BodyType = @event.Body.GetType().AssemblyQualifiedName,
                Body = JToken.FromObject(@event.Body)
            };

            if (@event.Metadata != null)
            {
                document.MetadataType = @event.Metadata.GetType().AssemblyQualifiedName;
                document.Metadata = JToken.FromObject(@event.Metadata);
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
                Body = JToken.FromObject(snapshot)
            };

            if (metadata != null)
            {
                document.Metadata = JToken.FromObject(metadata);
                document.MetadataType = metadata.GetType().AssemblyQualifiedName;
            }

            return document;
        }

        private EventData Deserialize(EventDocument document)
        {
            object metadata = null;

            if (!string.IsNullOrEmpty(document.MetadataType))
                metadata = document.Metadata.ToObject(this.TypeCache.Resolve(document.MetadataType));

            return new EventData(document.Body.ToObject(this.TypeCache.Resolve(document.BodyType)), metadata, document.Version);
        }

        private Snapshot Deserialize(SnapshotDocument document)
        {
            object metadata = null;

            if (!string.IsNullOrEmpty(document.MetadataType))
                metadata = document.Metadata.ToObject(this.TypeCache.Resolve(document.MetadataType));

            return new Snapshot(document.Body.ToObject(this.TypeCache.Resolve(document.BodyType)), metadata, document.Version);
        }
    }
}
