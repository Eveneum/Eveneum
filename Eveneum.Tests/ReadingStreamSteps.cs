using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Eveneum.Tests.Infrastrature;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace Eveneum.Tests
{
    [Binding]
    public class ReadingStreamSteps
    {
        private readonly CosmosDbContext Context;

        ReadingStreamSteps(CosmosDbContext context)
        {
            this.Context = context;
        }

        [When(@"I read stream ([^\s-])")]
        public async Task WhenIReadStream(string streamId)
        {
            this.Context.StreamId = streamId;

            var response = await this.Context.EventStore.ReadStream(streamId);

            this.Context.Stream = response.Stream;
            this.Context.Response = response;
        }

        [When(@"I read stream ([^\s-]) as of version (\d+)")]
        public async Task WhenIReadStreamAsOfVersion(string streamId, ulong version)
        {
            this.Context.StreamId = streamId;

            var response = await this.Context.EventStore.ReadStreamAsOfVersion(streamId, version);

            this.Context.Stream = response.Stream;
            this.Context.Response = response;
        }

        [When(@"I read stream ([^\s-]) from version (\d+)")]
        public async Task WhenIReadStreamFromVersion(string streamId, ulong version)
        {
            this.Context.StreamId = streamId;

            var response = await this.Context.EventStore.ReadStreamFromVersion(streamId, version);

            this.Context.Stream = response.Stream;
            this.Context.Response = response;
        }

        [When(@"I read stream ([^\s-]) ignoring snapshots")]
        public async Task WhenIReadStreamIgnoringSnapshots(string streamId)
        {
            this.Context.StreamId = streamId;

            var response = await this.Context.EventStore.ReadStreamIgnoringSnapshots(streamId);

            this.Context.Stream = response.Stream;
            this.Context.Response = response;
        }

        [Then(@"the non-existing stream is returned")]
        public void ThenTheNon_ExistingStreamIsReturned()
        {
            var stream = this.Context.Stream;

            Assert.IsFalse(stream.HasValue);
            Assert.IsFalse((this.Context.Response as StreamResponse).SoftDeleted);
        }

        [Then(@"the non-existing, soft-deleted stream is returned")]
        public void ThenTheNon_ExistingSoft_DeletedStreamIsReturned()
        {
            var stream = this.Context.Stream;

            Assert.IsFalse(stream.HasValue);
            Assert.IsTrue((this.Context.Response as StreamResponse).SoftDeleted);
        }

        [Then(@"the stream ([^\s-]) in version (\d+) is returned")]
        public void ThenTheStreamInVersionIsReturned(string streamId, ulong version)
        {
            var stream = this.Context.Stream;

            Assert.IsTrue(stream.HasValue);
            Assert.AreEqual(streamId, stream.Value.StreamId);
            Assert.AreEqual(version, stream.Value.Version);
            Assert.IsNull(stream.Value.Metadata);
        }

        [Then(@"the stream ([^\s-]) with metadata in version (\d+) is returned")]
        public void ThenTheStreamWithMetadataInVersionIsReturned(string streamId, ulong version)
        {
            var stream = this.Context.Stream;

            Assert.IsTrue(stream.HasValue);
            Assert.AreEqual(streamId, stream.Value.StreamId);
            Assert.AreEqual(version, stream.Value.Version);
            Assert.AreEqual(JToken.FromObject(this.Context.HeaderMetadata), JToken.FromObject(stream.Value.Metadata));
        }

        [Then(@"no snapshot is returned")]
        public void ThenNoSnapshotIsReturned()
        {
            var stream = this.Context.Stream;

            Assert.IsTrue(stream.HasValue);
            Assert.IsFalse(stream.Value.Snapshot.HasValue);
        }

        [Then(@"a snapshot for version (\d+) is returned")]
        public void ThenASnapshotForVersionIsReturned(ulong version)
        {
            var stream = this.Context.Stream;

            Assert.IsTrue(stream.HasValue);
            Assert.IsTrue(stream.Value.Snapshot.HasValue);
            Assert.AreEqual(version, stream.Value.Snapshot.Value.Version);
            Assert.AreEqual(JToken.FromObject(this.Context.Snapshot), JToken.FromObject(stream.Value.Snapshot.Value.Data));
            Assert.IsNull(stream.Value.Snapshot.Value.Metadata);
        }

        [Then(@"a snapshot with metadata for version (\d+) is returned")]
        public void ThenASnapshotWithMetadataForVersionIsReturned(ulong version)
        {
            var stream = this.Context.Stream;

            Assert.IsTrue(stream.HasValue);
            Assert.IsTrue(stream.Value.Snapshot.HasValue);
            Assert.AreEqual(version, stream.Value.Snapshot.Value.Version);
            Assert.AreEqual(JToken.FromObject(this.Context.Snapshot), JToken.FromObject(stream.Value.Snapshot.Value.Data));
            Assert.IsNotNull(stream.Value.Snapshot.Value.Metadata);
            Assert.AreEqual(JToken.FromObject(this.Context.SnapshotMetadata), JToken.FromObject(stream.Value.Snapshot.Value.Metadata));
        }

        [Then(@"no events are returned")]
        public void ThenNoEventsAreReturned()
        {
            var stream = this.Context.Stream;

            Assert.IsTrue(stream.HasValue);
            Assert.IsEmpty(stream.Value.Events);
        }

        [Then(@"events from version (\d+) to (\d+) are returned")]
        public async Task ThenEventsFromVersionToAreReturned(ulong fromVersion, ulong toVersion)
        {
            var stream = this.Context.Stream;
            var allDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, stream.Value.StreamId, Documents.DocumentType.Event);
            var eventDocuments = allDocuments.ToDictionary(x => x.Version);

            Assert.IsTrue(stream.HasValue);
            Assert.IsNotEmpty(stream.Value.Events);
            Assert.AreEqual(toVersion - fromVersion + 1, stream.Value.Events.Length);

            for(ulong version = fromVersion, index = 0; version <= toVersion; ++version, ++index)
            {
                var @event = stream.Value.Events[index];

                Assert.AreEqual(version, @event.Version);
                Assert.IsTrue(eventDocuments.ContainsKey(version));

                var eventDocument = eventDocuments[version];

                Assert.AreEqual(eventDocument.Metadata, JToken.FromObject(@event.Metadata));
            }
        }
    }
}
