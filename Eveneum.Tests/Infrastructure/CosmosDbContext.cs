using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Eveneum.Documents;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Eveneum.Tests.Infrastrature
{
    class CosmosDbContext : IDisposable
    {
        public string Database { get; private set; }
        public string Container { get; private set; }

        public CosmosClient Client { get; private set; }
        public IEventStore EventStore { get; private set; }
        public EventStoreOptions EventStoreOptions { get; } = new EventStoreOptions() { QueryMaxItemCount = 100 };

        public string StreamId { get; set; }
        public Stream? Stream { get; set; }
        public SampleMetadata HeaderMetadata { get; set; }
        public SampleSnapshot Snapshot { get; set; }
        public SampleMetadata SnapshotMetadata { get; set; }
        public EventData[] NewEvents { get; set; }
        public List<EventData> LoadAllEvents { get; set; }
        public List<StreamHeader> LoadAllStreamHeaders { get; set; }
        public EventData ReplacedEvent { get; set; }
        public List<EveneumDocument> ExistingDocuments { get; set; }

        public JsonSerializerSettings JsonSerializerSettings { get; set; } = new JsonSerializerSettings();

        public Response Response { get; set; }

        public CosmosDbContext()
        {
            this.Database = "EveneumDB";
            this.Container = Guid.NewGuid().ToString();
        }

        internal async Task Initialize(bool initializeEventStore = true)
        {
            this.JsonSerializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

            this.Client = await CosmosSetup.GetClient(this.Database, this.Container, this.JsonSerializerSettings);

            this.EventStoreOptions.JsonSerializer = JsonSerializer.Create(this.JsonSerializerSettings);
            this.EventStore = new EventStore(this.Client, this.Database, this.Container, this.EventStoreOptions);

            if (initializeEventStore)
                await this.EventStore.Initialize();
        }

        public void Dispose()
        {
            CosmosSetup.GetClient().GetDatabase(this.Database).GetContainer(this.Container).DeleteContainerAsync().Wait();
        }
    }
}
