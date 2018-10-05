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
                throw new KeyNotFoundException(streamId); // TODO: domain-specific exception needed

            var documents = new List<EveneumDocument>();
            var finishLoading = false;

            do
            {
                foreach (var document in page)
                {
                    var eveneumDoc = EveneumDocument.Parse(document);

                    if (eveneumDoc is HeaderDocument && eveneumDoc.Deleted)
                        throw new KeyNotFoundException(streamId); // TODO: domain-specific exception needed

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
            var events = documents.OfType<EventDocument>().Select(x => x.Body.ToObject(Type.GetType(x.Type))).Reverse().ToArray();
            var snapshot = documents.OfType<SnapshotDocument>().Select(x => new Snapshot(x.Body.ToObject(Type.GetType(x.Type)), x.Version)).FirstOrDefault();

            return new Stream(streamId, headerDocument.Version, headerDocument.Body.ToObject(Type.GetType(headerDocument.Type)), events, snapshot);
        }

        public async Task WriteSnapshot(string streamId, ulong version, object snapshot, bool deleteOlderSnapshots = false)
        {
            var document = new SnapshotDocument
            {
                Partition = this.Partition,
                StreamId = streamId,
                Version = version,
                Type = snapshot.GetType().AssemblyQualifiedName,
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

        public async Task WriteToStream(string streamId, object[] events, ulong expectedVersion = 0, object metadata = null)
        {
            var header = new HeaderDocument
            {
                Partition = this.Partition,
                StreamId = streamId,
                Version = expectedVersion + (ulong)events.Length
            };

            var headerUri = UriFactory.CreateDocumentUri(this.Database, this.Collection, header.Id);            

            string etag = null;

            // Existing stream
            if (expectedVersion > 0)
            {
                var existingHeader = await this.Client.ReadDocumentAsync<HeaderDocument>(headerUri, new RequestOptions { PartitionKey = this.PartitionKey });

                if (existingHeader.Document.Version != expectedVersion)
                    throw new OptimisticConcurrencyException(); // TODO: specific exception

                etag = existingHeader.Document.ETag;
            }

            var eventVersion = expectedVersion;

            if (metadata != null)
            {
                header.Body = JToken.FromObject(metadata);
                header.Type = metadata.GetType().AssemblyQualifiedName;
            }

            if (expectedVersion == 0)
            {
                await this.Client.CreateDocumentAsync(this.DocumentCollectionUri, header, new RequestOptions { PartitionKey = this.PartitionKey });
            }
            else
            {
                await this.Client.ReplaceDocumentAsync(headerUri, header, new RequestOptions { PartitionKey = this.PartitionKey, AccessCondition = new AccessCondition { Type = AccessConditionType.IfMatch, Condition = etag } });
            }

            var eventDocuments = (events ?? Enumerable.Empty<object>())
                .Select(@event => new EventDocument
                {                    
                    Partition = this.Partition,
                    StreamId = streamId,
                    Version = ++eventVersion,
                    Type = @event.GetType().AssemblyQualifiedName,
                    Body = JToken.FromObject(@event)
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
                throw new OptimisticConcurrencyException(); // TODO: specific exception

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
