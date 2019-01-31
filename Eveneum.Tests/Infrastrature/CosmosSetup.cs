using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eveneum.Documents;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;

namespace Eveneum.Tests.Infrastrature
{
    static class CosmosSetup
    {
        public static IDocumentClient GetClient()
        {
            var endpoint = Environment.GetEnvironmentVariable("CosmosDbEmulator.Endpoint", EnvironmentVariableTarget.User) ?? "https://localhost:8081";
            var key = Environment.GetEnvironmentVariable("CosmosDbEmulator.Key", EnvironmentVariableTarget.User) ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

            return new DocumentClient(new Uri(endpoint), key);
        }

        public static async Task<IDocumentClient> GetClient(string database, string collection = null, bool partitioned = false)
        {
            var client = GetClient();

            var databaseResponse = await client.CreateDatabaseIfNotExistsAsync(new Database { Id = database });

            if (collection != null)
            {
                var documentCollection = new DocumentCollection { Id = collection };

                if (partitioned)
                    documentCollection.PartitionKey = new PartitionKeyDefinition { Paths = new System.Collections.ObjectModel.Collection<string>(new List<string> { "/" + nameof(Documents.EveneumDocument.Partition) }) };

                await client.CreateDocumentCollectionIfNotExistsAsync(databaseResponse.Resource.CollectionsLink, documentCollection);
            }
            
            return client;
        }

        public static Task<List<EveneumDocument>> QueryAllDocuments(IDocumentClient client, string database, string collection)
            => Query<EveneumDocument>(client, database, collection, "SELECT * FROM x", new FeedOptions { EnableCrossPartitionQuery = true });

        public static Task<List<TDocument>> QueryAllDocumentsInStream<TDocument>(IDocumentClient client, string database, string collection, PartitionKey partitionKey, string streamId)
            where TDocument : EveneumDocument
            => Query<TDocument>(client, database, collection, $"SELECT * FROM x WHERE x.{nameof(EveneumDocument.StreamId)} = '{streamId}'", new FeedOptions { PartitionKey = partitionKey });

        private static async Task<List<TDocument>> Query<TDocument>(IDocumentClient client, string database, string collection, string query, FeedOptions feedOptions)
            where TDocument : EveneumDocument
        {
            var documentQuery = client.CreateDocumentQuery<Document>(UriFactory.CreateDocumentCollectionUri(database, collection), query, feedOptions).AsDocumentQuery();

            var documents = new List<TDocument>();

            do
            {
                var page = await documentQuery.ExecuteNextAsync<Document>();
                documents.AddRange(page.Select(x => EveneumDocument.Parse(x, new JsonSerializerSettings())).OfType<TDocument>());
            }
            while (documentQuery.HasMoreResults);

            return documents;
        }
    }
}
