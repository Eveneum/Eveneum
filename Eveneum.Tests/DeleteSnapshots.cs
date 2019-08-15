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
        public async Task DeletedSnapshotAppearsInChangeFeed()
        {
            // Arrange
            var partition = Guid.NewGuid().ToString();

            var client = await CosmosSetup.GetClient(this.Database, this.Collection);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents(10);

            await store.WriteToStream(streamId, events);
            await store.CreateSnapshot(streamId, 2, 2);
            await store.CreateSnapshot(streamId, 4, 4);
            await store.CreateSnapshot(streamId, 8, 8);

            // Act           
            //var token = await this.GetCurrentChangeFeedToken(client, partition);

            await store.DeleteSnapshots(streamId, 8);

            // Assert
            //var documents = await this.LoadChangeFeed(client, partition, token);

            //Assert.AreEqual(2, documents.Count);
            //var snapshots = documents.OfType<SnapshotDocument>().ToList();
            //Assert.AreEqual(2, snapshots.Count);
            
            //foreach(var snapshot in snapshots)
            //{
            //    Assert.IsTrue(snapshot.Deleted);
            //    Assert.Contains(snapshot.Version, new[] { 2, 4 });
            //}
        }
    }
}
