using Eveneum.Documents;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eveneum
{
    class EventStore : IEventStore
    {
        private readonly DocumentClient Client;
        private readonly string Collection;
        private readonly string Database;
        private readonly string Partition;
        private readonly PartitionKey PartitionKey;

        private readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings();

        public EventStore(DocumentClient client, string database, string collection, string partition = null)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            this.Client = client;
            this.Database = database;
            this.Collection = collection;
            this.Partition = string.IsNullOrEmpty(partition) ? null : partition;
            this.PartitionKey = string.IsNullOrEmpty(this.Partition) ? null : new PartitionKey(this.Partition);
        }

        public async Task<Stream> ReadStream(string streamId)
        {
            if (streamId == null)
                throw new ArgumentNullException(nameof(streamId));

            var sql = $"SELECT * FROM x WHERE x.{nameof(EveneumDocument.StreamId)} = '{streamId}' ORDER BY x.{nameof(EveneumDocument.SortOrder)} DESC";
            var query = this.Client.CreateDocumentQuery<Document>(UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection), sql, new FeedOptions { PartitionKey = this.PartitionKey }).AsDocumentQuery();

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

        public async Task WriteSnapshot(string streamId, ulong version, object snapshot, bool deletePrevious = false)
        {
            var document = new SnapshotDocument
            {
                Partition = this.Partition,
                StreamId = streamId,
                Version = version,
                Type = snapshot.GetType().AssemblyQualifiedName,
                Body = JToken.FromObject(snapshot)
            };

            var uri = UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection);
            await this.Client.UpsertDocumentAsync(uri, document, new RequestOptions { PartitionKey = this.PartitionKey }, true);

            if (deletePrevious)
                await this.DeleteSnapshots(streamId, version);
        }

        public async Task DeleteSnapshots(string streamId, ulong maxVersion)
        {
            var query = this.Client.CreateDocumentQuery<SnapshotDocument>(UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection), new FeedOptions { PartitionKey = this.PartitionKey })
                .Where(x => x.StreamId == streamId)
                .Where(x => x.DocumentType == DocumentType.Snapshot)
                .Where(x => x.Version < maxVersion)
                .AsDocumentQuery();

            var documents = new List<Document>();

            while(query.HasMoreResults)
            {
                var page = await query.ExecuteNextAsync<Document>();
                documents.AddRange(page);
            }

            var tasks = documents.Select(document => this.Client.DeleteDocumentAsync(document.SelfLink, new RequestOptions { PartitionKey = this.PartitionKey }));

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
            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection);

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
                await this.Client.CreateDocumentAsync(documentCollectionUri, header, new RequestOptions { PartitionKey = this.PartitionKey });
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
                await this.Client.CreateDocumentAsync(documentCollectionUri, eventDocument, new RequestOptions { PartitionKey = this.PartitionKey });
        }

        public async Task DeleteStream(Stream stream)
        {
        }
    }
}
