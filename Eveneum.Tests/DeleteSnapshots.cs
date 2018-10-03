using System;
using System.Linq;
using System.Threading.Tasks;
using Eveneum.Documents;
using Eveneum.Tests.Infrastrature;
using NUnit.Framework;

namespace Eveneum.Tests
{
    /// <summary>
    /// Soft-deletes snapshots up to a specific version
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

            Assert.AreEqual(1 + events.Length, allDocuments.Where(x => !x.Deleted).Count());
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

            Assert.AreEqual(1 + events.Length + 1, allDocuments.Where(x => !x.Deleted).Count());
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task DeletedSnapshotAppearsInChangeFeed(bool partitioned)
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
            var token = await this.GetCurrentChangeFeedToken(client, partition);

            await store.DeleteSnapshots(streamId, 8);

            // Assert
            var documents = await this.LoadChangeFeed(client, partition, token);

            Assert.AreEqual(2, documents.Count);
            var snapshots = documents.OfType<SnapshotDocument>().ToList();
            Assert.AreEqual(2, snapshots.Count);
            
            foreach(var snapshot in snapshots)
            {
                Assert.IsTrue(snapshot.Deleted);
                Assert.Contains(snapshot.Version, new[] { 2, 4 });
            }
        }
    }
}
