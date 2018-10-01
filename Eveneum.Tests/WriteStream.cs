using System;
using System.Linq;
using System.Threading.Tasks;
using Eveneum.Documents;
using Eveneum.Tests.Infrastrature;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Eveneum.Tests
{
    /// <summary>
    /// Writes a stream with a large number of events.
    /// </summary>
    public class WriteStream : CosmosTest
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task NewStream(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents(5);
            var metadata = TestSetup.GetMetadata();

            // Act
            await store.WriteToStream(streamId, events.Cast<object>().ToArray(), metadata: metadata);

            // Assert
            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + events.Count, allDocuments.Count);

            var headerDocument = allDocuments.OfType<HeaderDocument>().Single();
            Assert.AreEqual(streamId, headerDocument.Id);
            Assert.AreEqual(partition, headerDocument.Partition);
            Assert.AreEqual(DocumentType.Header, headerDocument.DocumentType);
            Assert.AreEqual(streamId, headerDocument.StreamId);
            Assert.AreEqual(typeof(SampleMetadata).AssemblyQualifiedName, headerDocument.Type);
            Assert.AreEqual((ulong)events.Count, headerDocument.Version);
            Assert.AreEqual(metadata.GetType().AssemblyQualifiedName, headerDocument.Type);
            Assert.NotNull(headerDocument.Body);
            Assert.AreEqual(JToken.FromObject(metadata), headerDocument.Body);
            Assert.NotNull(headerDocument.ETag);
            Assert.False(headerDocument.Deleted);
            Assert.AreEqual(events.Count + EveneumDocument.GetOrderingFraction(DocumentType.Header), headerDocument.SortOrder);

            foreach(var @event in events)
            {
                var eventDocument = allDocuments.OfType<EventDocument>().Single(x => x.Version == (uint)@event.Version);
                Assert.AreEqual(streamId + EveneumDocument.Separator + @event.Version.ToString(), eventDocument.Id);
                Assert.AreEqual(partition, eventDocument.Partition);
                Assert.AreEqual(DocumentType.Event, eventDocument.DocumentType);
                Assert.AreEqual(streamId, eventDocument.StreamId);
                Assert.AreEqual(@event.GetType().AssemblyQualifiedName, eventDocument.Type);
                Assert.NotNull(eventDocument.Body);
                Assert.AreEqual(JToken.FromObject(@event), eventDocument.Body);
                Assert.NotNull(eventDocument.ETag);
                Assert.False(eventDocument.Deleted);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task LongStream(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents(1000).Cast<object>().ToArray();

            // Act
            await store.WriteToStream(streamId, events);

            // Assert
            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + events.Length, allDocuments.Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task NoEvents(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = Array.Empty<object>();

            // Act
            await store.WriteToStream(streamId, events);

            // Assert
            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1, allDocuments.Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task FailsWhenStreamIdIsUsed(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var existingEvents = TestSetup.GetEvents(10).Cast<object>().ToArray();

            await store.WriteToStream(streamId, existingEvents);

            // Act
            var exception = Assert.CatchAsync<Exception>(() => store.WriteToStream(streamId, TestSetup.GetEvents().Cast<object>().ToArray())); // TODO: introduce specific exception

            // Assert
            Assert.NotNull(exception);

            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + existingEvents.Length, allDocuments.Count);
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
            var existingEvents = TestSetup.GetEvents(10).Cast<object>().ToArray();

            await store.WriteToStream(streamId, existingEvents);

            // Act
            var exception = Assert.CatchAsync<OptimisticConcurrencyException>(() => store.WriteToStream(streamId, TestSetup.GetEvents().Cast<object>().ToArray(), (ulong)existingEvents.Length - 1));

            // Assert
            Assert.NotNull(exception);

            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + existingEvents.Length, allDocuments.Count);
        }
    }
}
