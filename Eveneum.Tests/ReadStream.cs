using System;
using System.Linq;
using System.Threading.Tasks;
using Eveneum.Tests.Infrastrature;
using NUnit.Framework;

namespace Eveneum.Tests
{
    /// <summary>
    /// Reads an existing stream
    /// </summary>
    public class ReadStream : CosmosTest
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task EmptyStream(bool partitioned)
        {
            // Arrange
            var partition = partitioned ? Guid.NewGuid().ToString() : null;

            var client = await CosmosSetup.GetClient(this.Database, this.Collection, partitioned: partitioned);
            var store = new EventStore(client, this.Database, this.Collection, partition);

            var streamId = Guid.NewGuid().ToString();
            var events = Array.Empty<EventData>();
            var metadata = TestSetup.GetMetadata();

            await store.WriteToStream(streamId, events, metadata: metadata);

            // Act
            var stream = await store.ReadStream(streamId);

            // Assert
            Assert.IsNotNull(stream);
            Assert.AreEqual(streamId, stream.StreamId);
            Assert.IsEmpty(stream.Events);
            Assert.IsNull(stream.Snapshot);
        }
    }
}