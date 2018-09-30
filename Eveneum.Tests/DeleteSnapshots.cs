using System;
using System.Linq;
using System.Threading.Tasks;
using Eveneum.Tests.Infrastrature;
using NUnit.Framework;

namespace Eveneum.Tests
{
    /// <summary>
    /// Writes a stream with a large number of events.
    /// </summary>
    public class DeleteSnapshots : CosmosTest
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task DeletesAllSnapshots(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents(10).Cast<object>().ToArray();

            await store.WriteToStream(streamId, events);
            await store.WriteSnapshot(streamId, 2, 2);
            await store.WriteSnapshot(streamId, 4, 4);
            await store.WriteSnapshot(streamId, 8, 8);

            // Act
            await store.DeleteSnapshots(streamId, ulong.MaxValue);

            // Assert
            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + events.Length, allDocuments.Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task DeletesPreviousSnapshots(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents(10).Cast<object>().ToArray();

            await store.WriteToStream(streamId, events);
            await store.WriteSnapshot(streamId, 2, 2);
            await store.WriteSnapshot(streamId, 4, 4);
            await store.WriteSnapshot(streamId, 8, 8);

            // Act
            await store.DeleteSnapshots(streamId, 8);

            // Assert
            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + events.Length + 1, allDocuments.Count);
        }
    }
}
