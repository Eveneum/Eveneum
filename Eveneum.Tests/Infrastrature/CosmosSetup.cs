using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eveneum.Documents;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace Eveneum.Tests.Infrastrature
{
    static class CosmosSetup
    {
        public static DocumentClient GetClient()
        {
            var endpoint = Environment.GetEnvironmentVariable("CosmosDbEmulator.Endpoint", EnvironmentVariableTarget.User) ?? "https://localhost:8081";
            var key = Environment.GetEnvironmentVariable("CosmosDbEmulator.Key", EnvironmentVariableTarget.User) ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

            return new DocumentClient(new Uri(endpoint), key);
        }

        public static async Task<DocumentClient> GetClient(string database, string collection = null, bool partitioned = false)
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

        public static async Task<List<EveneumDocument>> QueryAllDocuments(DocumentClient client, string database, string collection)
        {
            var query = client.CreateDocumentQuery<Document>(UriFactory.CreateDocumentCollectionUri(database, collection), "SELECT * FROM x", new FeedOptions { EnableCrossPartitionQuery = true }).AsDocumentQuery();

            var documents = new List<EveneumDocument>();

            do
            {
                var page = await query.ExecuteNextAsync<Document>();
                documents.AddRange(page.Select(EveneumDocument.Parse));
            }
            while (query.HasMoreResults);

            return documents;
        }
    }
}
