using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eveneum.Documents;
using Microsoft.Azure.Cosmos;

namespace Eveneum.Tests.Infrastrature
{
    static class CosmosSetup
    {
        public static CosmosClient GetClient()
        {
            var endpoint = Environment.GetEnvironmentVariable("CosmosDbEmulator.Endpoint", EnvironmentVariableTarget.User) ?? "https://localhost:8081";
            var key = Environment.GetEnvironmentVariable("CosmosDbEmulator.Key", EnvironmentVariableTarget.User) ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

            return new CosmosClient(endpoint, key);
        }

        public static async Task<CosmosClient> GetClient(string database, string container = null)
        {
            var client = GetClient();

            await client.CreateDatabaseIfNotExistsAsync(database);

            if (container != null)
                await client.GetDatabase(database).CreateContainerAsync(new ContainerProperties(container, "/" + nameof(EveneumDocument.StreamId)));
            
            return client;
        }

        public static Task<List<EveneumDocument>> QueryAllDocuments(CosmosClient client, string database, string collection)
            => Query(client, database, collection, "SELECT * FROM x");

        public static Task<List<EveneumDocument>> QueryAllDocumentsInStream(CosmosClient client, string database, string collection, string streamId, DocumentType? documentType = null)
            => Query(client, database, collection, $"SELECT * FROM x", new PartitionKey(streamId), documentType);

        private static async Task<List<EveneumDocument>> Query(CosmosClient client, string database, string collection, string query, PartitionKey? partitionKey = null, DocumentType? documentType = null)
        {
            var documentQuery = client.GetDatabase(database).GetContainer(collection).GetItemQueryIterator<EveneumDocument>(query, requestOptions: new QueryRequestOptions { PartitionKey = partitionKey });

            var documents = new List<EveneumDocument>();

            do
            {
                var page = await documentQuery.ReadNextAsync();
                documents.AddRange(page.Where(x => !documentType.HasValue || x.DocumentType == documentType.Value));
            }
            while (documentQuery.HasMoreResults);

            return documents;
        }
    }
}
