using System;
using System.Threading.Tasks;
using Eveneum.Documents;
using Microsoft.Azure.Cosmos;
using NUnit.Framework;

namespace Eveneum.Tests
{
    [TestFixture]
    public class Pad
    {
        protected string Database { get; private set; } = "EveneumDB";
        protected string Collection { get; private set; } = Guid.NewGuid().ToString();

        [Test]
        public async Task Test()
        {
            //Setup
            var endpoint = "https://localhost:8081";
            var key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            var client = new CosmosClient(endpoint, key);

            await client.CreateDatabaseIfNotExistsAsync(this.Database);
            await client.GetDatabase(this.Database).CreateContainerAsync(new ContainerProperties(this.Collection, "/" + nameof(EveneumDocument.StreamId)));

            IEventStore eventStore = new EventStore(client, this.Database, this.Collection);
            await eventStore.Initialize();

            // Test
            var streamId = Guid.NewGuid().ToString();
            
            EventData[] events = new EventData[] 
            {
                new EventData(streamId, "Event 1", null, 1),
                new EventData(streamId, "Event 2", null, 2),
                new EventData(streamId, "Event 3", null, 3)
            };

            await eventStore.WriteToStream(streamId, events);

            // Expected version is the number of events written before
            var expectedVersion = (ulong)events.Length;

            events = new EventData[]
            {
                new EventData(streamId, "Event 4", null, 4),
                new EventData(streamId, "Event 5", null, 5)
            };

            await eventStore.WriteToStream(streamId, events, expectedVersion);

            // Expected version is the version returned when reading the stream
            var stream = await eventStore.ReadStream(streamId);
            expectedVersion = stream.Stream.Value.Version;

            events = new EventData[]
            {
                new EventData(streamId, "Event 6", null, 6),
                new EventData(streamId, "Event 7", null, 7)
            };

            await eventStore.WriteToStream(streamId, events, expectedVersion);
        }
    }
}
