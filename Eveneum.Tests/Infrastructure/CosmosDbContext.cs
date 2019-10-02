using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eveneum.Documents;
using Eveneum.Serialization;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;

namespace Eveneum.Tests.Infrastrature
{
    class CosmosDbContext : IDisposable
    {
        public string Database { get; private set; }
        public string Container { get; private set; }

        public CosmosClient Client { get; private set; }
        public IEventStore EventStore { get; private set; }
        public EventStoreOptions EventStoreOptions { get; } = new EventStoreOptions();

        public string StreamId { get; set; }
        public Stream? Stream { get; set; }
        public SampleMetadata HeaderMetadata { get; set; }
        public SampleSnapshot Snapshot { get; set; }
        public SampleMetadata SnapshotMetadata { get; set; }
        public EventData[] NewEvents { get; set; }
        public List<EventData> LoadAllEvents { get; set; }
        public List<EveneumDocument> ExistingDocuments { get; set; }

        public double RequestCharge { get; set; }

        public CosmosDbContext()
        {
            this.Database = "EveneumDB";
            this.Container = Guid.NewGuid().ToString();
        }

        internal async Task Initialize(bool initializeEventStore = true)
        {
            this.Client = await CosmosSetup.GetClient(this.Database, this.Container);
            this.EventStore = new EventStore(CosmosSetup.GetClientBuilder(), this.Database, this.Container, this.EventStoreOptions);

            if (initializeEventStore)
                await this.EventStore.Initialize();
        }

        public void Dispose()
        {
            CosmosSetup.GetClient().GetDatabase(this.Database).GetContainer(this.Container).DeleteContainerAsync().Wait();
        }
    }
}
