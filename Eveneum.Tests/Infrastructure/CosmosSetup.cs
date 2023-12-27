using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eveneum.Documents;
using Eveneum.Serialization;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Eveneum.Tests.Infrastructure
{
    static class CosmosSetup
    {
        public static CosmosClient GetClient(JsonSerializerSettings serializerSettings = null)
        {
            var endpoint = Environment.GetEnvironmentVariable("CosmosDbEmulator.Endpoint", EnvironmentVariableTarget.User) ?? "https://localhost:8081";
            var key = Environment.GetEnvironmentVariable("CosmosDbEmulator.Key", EnvironmentVariableTarget.User) ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

            return new CosmosClient(endpoint, key, new CosmosClientOptions { Serializer = new JsonNetCosmosSerializer(JsonSerializer.Create(serializerSettings ?? new JsonSerializerSettings())) });
        }

        public static async Task<CosmosClient> GetClient(string database, string container = null, JsonSerializerSettings serializerSettings = null)
        {
            var client = GetClient(serializerSettings);

            await client.CreateDatabaseIfNotExistsAsync(database);

            if (container != null)
            {
                var containerProperties = new ContainerProperties(container, "/" + nameof(EveneumDocument.StreamId)) { DefaultTimeToLive = -1 };
                await client.GetDatabase(database).CreateContainerAsync(containerProperties);
            }
            
            return client;
        }

        public static Task<List<EveneumDocument>> QueryAllDocuments(CosmosClient client, string database, string collection)
            => Query(client, database, collection, "SELECT * FROM x");

        public static Task<List<EveneumDocument>> QueryAllDocumentsInStream(CosmosClient client, string database, string collection, string streamId, DocumentType? documentType = null)
            => Query(client, database, collection, $"SELECT * FROM x", new PartitionKey(streamId), documentType);

        private static async Task<List<EveneumDocument>> Query(CosmosClient client, string database, string collection, string query, PartitionKey? partitionKey = null, DocumentType? documentType = null)
        {
            using var documentQuery = client.GetDatabase(database).GetContainer(collection).GetItemQueryIterator<EveneumDocument>(query, requestOptions: new QueryRequestOptions { PartitionKey = partitionKey });

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
