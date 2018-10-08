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
    /// Writes a new stream or appends to an existing one
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
            var events = TestSetup.GetEvents();
            var metadata = TestSetup.GetMetadata();

            // Act
            await store.WriteToStream(streamId, events, metadata: metadata);

            // Assert
            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + events.Length, allDocuments.Count);

            var headerDocument = allDocuments.OfType<HeaderDocument>().Single();
            Assert.AreEqual(streamId, headerDocument.Id);
            Assert.AreEqual(partition, headerDocument.Partition);
            Assert.AreEqual(DocumentType.Header, headerDocument.DocumentType);
            Assert.AreEqual(streamId, headerDocument.StreamId);
            Assert.AreEqual(typeof(SampleMetadata).AssemblyQualifiedName, headerDocument.MetadataType);
            Assert.AreEqual((ulong)events.Length, headerDocument.Version);
            Assert.AreEqual(metadata.GetType().AssemblyQualifiedName, headerDocument.MetadataType);
            Assert.NotNull(headerDocument.Metadata);
            Assert.AreEqual(JToken.FromObject(metadata), headerDocument.Metadata);
            Assert.NotNull(headerDocument.ETag);
            Assert.False(headerDocument.Deleted);
            Assert.AreEqual(events.Length + EveneumDocument.GetOrderingFraction(DocumentType.Header), headerDocument.SortOrder);

            foreach(var @event in events)
            {
                var eventDocument = allDocuments.OfType<EventDocument>().Single(x => x.Version == (uint)@event.Version);
                Assert.AreEqual(streamId + EveneumDocument.Separator + @event.Version.ToString(), eventDocument.Id);
                Assert.AreEqual(partition, eventDocument.Partition);
                Assert.AreEqual(DocumentType.Event, eventDocument.DocumentType);
                Assert.AreEqual(streamId, eventDocument.StreamId);
                Assert.AreEqual(@event.Body.GetType().AssemblyQualifiedName, eventDocument.BodyType);
                Assert.NotNull(eventDocument.Body);
                Assert.AreEqual(JToken.FromObject(@event.Body), eventDocument.Body);
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
            var events = TestSetup.GetEvents(1000);

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
            var events = Array.Empty<EventData>();

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
            var existingEvents = TestSetup.GetEvents(10);

            await store.WriteToStream(streamId, existingEvents);

            // Act
            var exception = Assert.CatchAsync<Exception>(() => store.WriteToStream(streamId, TestSetup.GetEvents())); // TODO: introduce specific exception

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
            var existingEvents = TestSetup.GetEvents(10);

            await store.WriteToStream(streamId, existingEvents);

            // Act
            var exception = Assert.CatchAsync<OptimisticConcurrencyException>(() => store.WriteToStream(streamId, TestSetup.GetEvents(), (ulong)existingEvents.Length - 1));

            // Assert
            Assert.NotNull(exception);
            Assert.AreEqual(streamId, exception.StreamId);
            Assert.AreEqual(existingEvents.Length - 1, exception.ExpectedVersion);
            Assert.AreEqual(existingEvents.Length, exception.ActualVersion);

            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + existingEvents.Length, allDocuments.Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task AppendsEventsToExistingStream(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents();
            var newEvents = TestSetup.GetEvents(startVersion: events.Length + 1);

            await store.WriteToStream(streamId, events);

            // Act
            await store.WriteToStream(streamId, newEvents, (ulong)events.Length);

            // Assert
            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + events.Length + newEvents.Length, allDocuments.Count);

            foreach (var @event in newEvents)
            {
                var eventDocument = allDocuments.OfType<EventDocument>().Single(x => x.Version == (uint)@event.Version);
                Assert.AreEqual(streamId + EveneumDocument.Separator + @event.Version.ToString(), eventDocument.Id);
                Assert.AreEqual(partition, eventDocument.Partition);
                Assert.AreEqual(DocumentType.Event, eventDocument.DocumentType);
                Assert.AreEqual(streamId, eventDocument.StreamId);
                Assert.AreEqual(@event.Body.GetType().AssemblyQualifiedName, eventDocument.BodyType);
                Assert.NotNull(eventDocument.Body);
                Assert.AreEqual(JToken.FromObject(@event.Body), eventDocument.Body);
                Assert.NotNull(eventDocument.ETag);
                Assert.False(eventDocument.Deleted);
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task AppendsNoEventsToExistingStream(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents();

            await store.WriteToStream(streamId, events);

            // Act
            await store.WriteToStream(streamId, Array.Empty<EventData>(), (ulong)events.Length);

            // Assert
            var allDocuments = await CosmosSetup.QueryAllDocuments(client, this.Database, this.Collection);

            Assert.AreEqual(1 + events.Length, allDocuments.Count);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task FailsAppendingWhenExpectedVersionDoesntMatch(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = TestSetup.GetEvents();
            var newEvents = TestSetup.GetEvents(startVersion: events.Length + 1);

            await store.WriteToStream(streamId, events);

            // Act
            var exception = Assert.ThrowsAsync<OptimisticConcurrencyException>(() => store.WriteToStream(streamId, newEvents, (ulong)events.Length - 2));

            // Assert
            Assert.NotNull(exception);
            Assert.AreEqual(streamId, exception.StreamId);
            Assert.AreEqual(events.Length - 2, exception.ExpectedVersion);
            Assert.AreEqual(events.Length, exception.ActualVersion);
        }
    }
}
