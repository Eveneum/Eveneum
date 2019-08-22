using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eveneum.Documents;
using Microsoft.Azure.Cosmos;

namespace Eveneum.Tests.Infrastrature
{
    class CosmosDbContext : IDisposable
    {
        public string Database { get; private set; }
        public string Collection { get; private set; }

        public CosmosClient Client { get; private set; }
        public IEventStore EventStore { get; private set; }

        public string StreamId { get; set; }
        public Stream? Stream { get; set; }
        public SampleMetadata HeaderMetadata { get; set; }
        public SampleSnapshot Snapshot { get; set; }
        public SampleMetadata SnapshotMetadata { get; set; }
        public EventData[] NewEvents { get; set; }
        public List<EventData> LoadAllEvents { get; set; }
        public List<EveneumDocument> ExistingDocuments { get; set; }

        public CosmosDbContext()
        {
            this.Database = "EveneumDB";
            this.Collection = Guid.NewGuid().ToString();
        }

        internal async Task Initialize()
        {
            this.Client = await CosmosSetup.GetClient(this.Database, this.Collection);
            this.EventStore = new EventStore(this.Client, this.Database, this.Collection);
        }

        public void Dispose()
        {
            CosmosSetup.GetClient().GetDatabase(this.Database).GetContainer(this.Collection).DeleteContainerAsync().Wait();
        }
    }
}
