using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eveneum.Documents;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using NUnit.Framework;

namespace Eveneum.Tests.Infrastrature
{
    /// <summary>
    /// Base class for integration tests that use CosmosDB. Each test will execute against a new Collection.
    /// </summary>
    [TestFixture]
    public class CosmosTest
    {
        protected string Database { get; private set; }
        protected string Collection { get; private set; }

        [SetUp]
        public void SetUp()
        {
            this.Database = "EveneumDB";
            this.Collection = Guid.NewGuid().ToString();
        }

        [TearDown]
        public async Task TearDown()
        {
            await CosmosSetup.GetClient().DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection));
        }

        internal async Task<IReadOnlyCollection<PartitionKeyRange>> GetPartitionKeyRanges(IDocumentClient client)
        {
            string responseContinuation = null;
            var partitionKeyRanges = new List<PartitionKeyRange>();

            var documentCollectionUri = UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection);

            do
            {
                var response = await client.ReadPartitionKeyRangeFeedAsync(documentCollectionUri, new FeedOptions { RequestContinuation = responseContinuation });

                partitionKeyRanges.AddRange(response);
                responseContinuation = response.ResponseContinuation;
            }
            while (responseContinuation != null);

            return partitionKeyRanges;
        }

        internal async Task<string> GetCurrentChangeFeedToken(IDocumentClient client, string partition)
        {
            PartitionKeyRange partitionKeyRange = partition == null ? (await this.GetPartitionKeyRanges(client)).FirstOrDefault() : null;

            var changeFeed = client.CreateDocumentChangeFeedQuery(UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection),
                new ChangeFeedOptions
                {
                    PartitionKeyRangeId = partitionKeyRange?.Id,
                    PartitionKey = partition != null ? new PartitionKey(partition) : null,
                    StartFromBeginning = true
                });

            string token = null;

            while (changeFeed.HasMoreResults)
            {
                var page = await changeFeed.ExecuteNextAsync<Document>();
                token = page.ResponseContinuation;
            }

            return token;
        }

        internal async Task<IReadOnlyCollection<EveneumDocument>> LoadChangeFeed(IDocumentClient client, string partition, string token = null)
        {
            var partitioned = partition != null;

            PartitionKeyRange partitionKeyRange = partitioned ? null : (await this.GetPartitionKeyRanges(client)).FirstOrDefault();
            var changeFeed = client.CreateDocumentChangeFeedQuery(UriFactory.CreateDocumentCollectionUri(this.Database, this.Collection),
                new ChangeFeedOptions
                {
                    PartitionKeyRangeId = partitionKeyRange?.Id,
                    PartitionKey = partitioned ? new PartitionKey(partition) : null,
                    RequestContinuation = token
                });

            return await changeFeed.All();
        }
    }
}
