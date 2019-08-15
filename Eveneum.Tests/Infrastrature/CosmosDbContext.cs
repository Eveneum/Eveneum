using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Eveneum.Tests.Infrastrature
{
    class CosmosDbContext : IDisposable
    {
        public string Database { get; private set; }
        public string Collection { get; private set; }

        public CosmosClient Client { get; private set; }
        public IEventStore EventStore { get; private set; }
        public string Partition { get; set; }
        public PartitionKey PartitionKey => new PartitionKey(this.Partition);

        public CosmosDbContext()
        {
            this.Database = "EveneumDB";
            this.Collection = Guid.NewGuid().ToString();
        }

        internal async Task Initialize()
        {
            this.Client = await CosmosSetup.GetClient(this.Database, this.Collection);
            this.EventStore = new EventStore(this.Client, this.Database, this.Collection, this.Partition);
        }

        public void Dispose()
        {
            CosmosSetup.GetClient().GetDatabase(this.Database).GetContainer(this.Collection).DeleteContainerAsync().Wait();
        }
    }
}
