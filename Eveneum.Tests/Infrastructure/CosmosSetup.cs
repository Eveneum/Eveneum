using Eveneum.Documents;
using Eveneum.NewtonsoftJson.Serialization;
using Eveneum.Serialization;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Eveneum.Tests.Infrastructure
{
    static class CosmosSetup
    {
        public static CosmosClient GetClientWithNewtonsoftJson(JsonSerializerSettings serializerSettings = null) =>
            GetClient(new JsonNetCosmosSerializer(Newtonsoft.Json.JsonSerializer.Create(serializerSettings ?? new JsonSerializerSettings())));

        public static CosmosClient GetClientWithSystemTextJson(JsonSerializerOptions serializerOptions = null) =>
            GetClient(new SystemTextJsonCosmosSerializer(serializerOptions ?? new JsonSerializerOptions()));

        public static async Task<CosmosClient> GetClientWithNewtonsoftJson(string database, string container = null, JsonSerializerSettings serializerSettings = null)
        {
            var client = GetClientWithNewtonsoftJson(serializerSettings);

            await CreateContainer(database, container, client);

            return client;
        }

        public static async Task<CosmosClient> GetClientWithSystemTextJson(string database, string container = null, JsonSerializerOptions serializerOptions = null)
        {
            var client = GetClientWithSystemTextJson(serializerOptions);

            await CreateContainer(database, container, client);

            return client;
        }

        public static Task<List<IEveneumDocument>> QueryAllDocuments(CosmosClient client, string database, string collection)
            => Query(client, database, collection, "SELECT * FROM x");

        public static Task<List<IEveneumDocument>> QueryAllDocumentsInStream(CosmosClient client, string database, string collection, string streamId, DocumentType? documentType = null)
            => Query(client, database, collection, $"SELECT * FROM x", new PartitionKey(streamId), documentType);

        private static CosmosClient GetClient(CosmosSerializer serializer)
        {
            var endpoint = Environment.GetEnvironmentVariable("CosmosDbEmulator.Endpoint", EnvironmentVariableTarget.User) ?? "https://localhost:8081";
            var key = Environment.GetEnvironmentVariable("CosmosDbEmulator.Key", EnvironmentVariableTarget.User) ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

            return new CosmosClient(endpoint, key, new CosmosClientOptions
            {
                Serializer = serializer,
                RequestTimeout = TimeSpan.FromMinutes(1),
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromMinutes(1)
            });
        }

        private static async Task<List<IEveneumDocument>> Query(CosmosClient client, string database, string collection, string query, PartitionKey? partitionKey = null, DocumentType? documentType = null)
        {
            using var documentQuery = client.GetDatabase(database).GetContainer(collection).GetItemQueryIterator<IEveneumDocument>(query, requestOptions: new QueryRequestOptions { PartitionKey = partitionKey });

            var documents = new List<IEveneumDocument>();

            do
            {
                var page = await documentQuery.ReadNextAsync();
                documents.AddRange(page.Where(x => !documentType.HasValue || x.DocumentType == documentType.Value));
            }
            while (documentQuery.HasMoreResults);

            return documents;
        }

        private static async Task CreateContainer(string database, string container, CosmosClient client)
        {
            await client.CreateDatabaseIfNotExistsAsync(database);

            var containerProperties = new ContainerProperties(container, "/" + nameof(EveneumDocument.StreamId)) { DefaultTimeToLive = -1 };

            await client.GetDatabase(database).CreateContainerIfNotExistsAsync(containerProperties);
        }
    }
}
