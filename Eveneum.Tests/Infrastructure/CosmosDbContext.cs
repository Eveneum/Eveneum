using Eveneum.Documents;
using Eveneum.Snapshots;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eveneum.Tests.Infrastructure
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
        public SnapshotWriterSnapshot SnapshotWriterSnapshot { get; set; }
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

        internal void AddCamelCasePropertyNamesContractResolver()
        {
            var contractResolver = new CamelCasePropertyNamesContractResolver();
            contractResolver.NamingStrategy.OverrideSpecifiedNames = false;

            this.JsonSerializerSettings.ContractResolver = contractResolver;
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
