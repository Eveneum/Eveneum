using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json.Linq;
using Eveneum.Documents;

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

        public EventStore(DocumentClient client, string database, string collection, string partition = null)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
            this.Database = database ?? throw new ArgumentNullException(nameof(database));
            this.Collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this.Partition = string.IsNullOrEmpty(partition) ? null : partition;
            this.PartitionKey = string.IsNullOrEmpty(this.Partition) ? null : new PartitionKey(this.Partition);

            this.DocumentCollectionUri = UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection);
        }

        public async Task<Stream> ReadStream(string streamId)
        {
            if (streamId == null)
                throw new ArgumentNullException(nameof(streamId));

            var sql = $"SELECT * FROM x WHERE x.{nameof(EveneumDocument.StreamId)} = '{streamId}' ORDER BY x.{nameof(EveneumDocument.SortOrder)} DESC";
            var query = this.Client.CreateDocumentQuery<Document>(this.DocumentCollectionUri, sql, new FeedOptions { PartitionKey = this.PartitionKey }).AsDocumentQuery();

            var page = await query.ExecuteNextAsync<Document>();
            
            if (page.Count == 0)
                throw new StreamNotFoundException(streamId);

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

                page = await query.ExecuteNextAsync<Document>();
            }
            while (query.HasMoreResults);

            var headerDocument = documents.First() as HeaderDocument;
            var events = documents.OfType<EventDocument>().Select(x => x.Body.ToObject(Type.GetType(x.BodyType))).Reverse().ToArray();
            var snapshot = documents.OfType<SnapshotDocument>().Select(x => new Snapshot(x.Body.ToObject(Type.GetType(x.BodyType)), x.Version)).FirstOrDefault();

            return new Stream(streamId, headerDocument.Version, headerDocument.Metadata.ToObject(Type.GetType(headerDocument.MetadataType)), events, snapshot);
        }

        public async Task WriteSnapshot(string streamId, ulong version, object snapshot, object metadata = null, bool deleteOlderSnapshots = false)
        {
            var document = new SnapshotDocument
            {
                Partition = this.Partition,
                StreamId = streamId,
                Version = version,
                BodyType = snapshot.GetType().AssemblyQualifiedName,
                Body = JToken.FromObject(snapshot)
            };

            await this.Client.UpsertDocumentAsync(this.DocumentCollectionUri, document, new RequestOptions { PartitionKey = this.PartitionKey }, true);

            if (deleteOlderSnapshots)
                await this.DeleteSnapshots(streamId, version);
        }

        public async Task DeleteSnapshots(string streamId, ulong olderThanVersion)
        {
            var query = this.Client.CreateDocumentQuery<SnapshotDocument>(this.DocumentCollectionUri, new FeedOptions { PartitionKey = this.PartitionKey })
                .Where(x => x.StreamId == streamId)
                .Where(x => x.DocumentType == DocumentType.Snapshot)
                .Where(x => x.Version < olderThanVersion)
                .AsDocumentQuery();

            var documents = new List<SnapshotDocument>();

            while(query.HasMoreResults)
            {
                var page = await query.ExecuteNextAsync<SnapshotDocument>();
                documents.AddRange(page);
            }

            var tasks = documents.Select(async document =>
            {
                document.Deleted = true;
                
                await this.Client.UpsertDocumentAsync(this.DocumentCollectionUri, document,new RequestOptions { PartitionKey = this.PartitionKey });
            });

            await Task.WhenAll(tasks);
        }

        public async Task WriteToStream(string streamId, EventData[] events, ulong? expectedVersion = null, object metadata = null)
        {

            var headerUri = UriFactory.CreateDocumentUri(this.Database, this.Collection, HeaderDocument.GenerateId(streamId));            

            string etag = null;

            // Existing stream
            if (expectedVersion.HasValue && expectedVersion.Value > 0)
            {
                var existingHeader = await this.Client.ReadDocumentAsync<HeaderDocument>(headerUri, new RequestOptions { PartitionKey = this.PartitionKey });

                if (existingHeader.Document.Version != expectedVersion)
                    throw new OptimisticConcurrencyException(streamId, expectedVersion.Value, existingHeader.Document.Version);

                etag = existingHeader.Document.ETag;
            }

            var header = new HeaderDocument
            {
                Partition = this.Partition,
                StreamId = streamId,
                Version = (expectedVersion ?? 0) + (ulong)events.Length
            };

            if (metadata != null)
            {
                header.Metadata = JToken.FromObject(metadata);
                header.MetadataType = metadata.GetType().AssemblyQualifiedName;
            }

            if (!expectedVersion.HasValue)
            {
                try
                {
                    await this.Client.CreateDocumentAsync(this.DocumentCollectionUri, header, new RequestOptions { PartitionKey = this.PartitionKey });
                }
                catch(DocumentClientException ex) when (ex.Error.Code == nameof(System.Net.HttpStatusCode.Conflict))
                {
                    throw new StreamAlreadyExistsException(streamId);
                }
            }
            else
            {
                await this.Client.ReplaceDocumentAsync(headerUri, header, new RequestOptions { PartitionKey = this.PartitionKey, AccessCondition = new AccessCondition { Type = AccessConditionType.IfMatch, Condition = etag } });
            }

            var eventDocuments = (events ?? Enumerable.Empty<EventData>())
                .Select(@event => new EventDocument
                {                    
                    Partition = this.Partition,
                    StreamId = streamId,
                    Version = @event.Version,
                    BodyType = @event.Body.GetType().AssemblyQualifiedName,
                    Body = JToken.FromObject(@event.Body)
                });

            foreach(var eventDocument in eventDocuments)
                await this.Client.CreateDocumentAsync(this.DocumentCollectionUri, eventDocument, new RequestOptions { PartitionKey = this.PartitionKey });
        }

        public async Task DeleteStream(string streamId, ulong expectedVersion)
        {
            var header = new HeaderDocument
            {
                Partition = this.Partition,
                StreamId = streamId,
                Version = expectedVersion
            };

            var headerUri = UriFactory.CreateDocumentUri(this.Database, this.Collection, header.Id);

            HeaderDocument existingHeader;

            try
            {
                existingHeader = await this.Client.ReadDocumentAsync<HeaderDocument>(headerUri, new RequestOptions { PartitionKey = this.PartitionKey });
            }
            catch(DocumentClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new StreamNotFoundException(streamId);
            }

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
                var page = await query.ExecuteNextAsync<Document>();

                foreach(var document in page)
                {
                    var doc = EveneumDocument.Parse(document);
                    doc.Deleted = true;

                    await this.Client.UpsertDocumentAsync(this.DocumentCollectionUri, doc, new RequestOptions { PartitionKey = this.PartitionKey });
                }
            }
        }
    }
}
