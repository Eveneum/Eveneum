using System;
using System.Linq;
using System.Threading.Tasks;
using Eveneum.Tests.Infrastrature;
using NUnit.Framework;

namespace Eveneum.Tests
{
    /// <summary>
    /// Deletes the whole stream
    /// </summary>
    public class DeleteStream : CosmosTest
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task DeletesWholeStreamWithSnapshots(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents(200);

            await store.WriteToStream(streamId, events);
            await store.WriteSnapshot(streamId, 10, 10);
            await store.WriteSnapshot(streamId, 100, 100);
            await store.WriteSnapshot(streamId, 150, 150);

            // Act
            await store.DeleteStream(streamId, (ulong)events.Length);

            // Assert
            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(0, allDocuments.Where(x => !x.Deleted).Count());
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task DeletedDocumentsAppearInChangeFeed(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents(10);

            await store.WriteToStream(streamId, events);
            await store.WriteSnapshot(streamId, 2, 2);
            await store.WriteSnapshot(streamId, 4, 4);
            await store.WriteSnapshot(streamId, 8, 8);

            // Act           
            var token = await this.GetCurrentChangeFeedToken(client, partition);

            await store.DeleteStream(streamId, (ulong)events.Length);

            // Assert
            var documents = await this.LoadChangeFeed(client, partition, token);

            Assert.AreEqual(1 + events.Length + 3, documents.Count);
            
            foreach(var document in documents)
            {
                Assert.IsTrue(document.Deleted);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task FailsWhenExpectedVersionDoesntMatch(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents();

            await store.WriteToStream(streamId, events);

            // Act
            var exception = Assert.CatchAsync<OptimisticConcurrencyException>(() => store.DeleteStream(streamId, (ulong)events.Length + 1));

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(streamId, exception.StreamId);
            Assert.AreEqual(events.Length + 1, exception.ExpectedVersion);
            Assert.AreEqual(events.Length, exception.ActualVersion);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task FailsWhenStreamDoesntExist(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();

            // Act
            var exception = Assert.CatchAsync<StreamNotFoundException>(() => store.DeleteStream(streamId, 0));

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(streamId, exception.StreamId);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task FailsWhenStreamAlreadyDeleted(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents();

            await store.WriteToStream(streamId, events);
            await store.DeleteStream(streamId, (ulong)events.Length);

            // Act
            var exception = Assert.CatchAsync<StreamNotFoundException>(() => store.DeleteStream(streamId, (ulong)events.Length));

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(streamId, exception.StreamId);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task DoesntDeleteOtherStreams(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            await store.WriteToStream(streamId, TestSetup.GetEvents(10));

            var otherStreamId = Guid.NewGuid().ToString();
            await store.WriteToStream(otherStreamId, TestSetup.GetEvents());

            // Act
            await store.DeleteStream(streamId, 10);

            // Assert
            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + 5, allDocuments.Where(x => x.StreamId == otherStreamId && !x.Deleted).Count());
        }
    }
}
