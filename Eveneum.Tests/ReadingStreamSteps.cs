using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Eveneum.Tests.Infrastructure;
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

            var response = await this.Context.EventStore.ReadStream(streamId, new ReadStreamOptions { ToVersion = version });

            this.Context.Stream = response.Stream;
            this.Context.Response = response;
        }

        [When(@"I read stream ([^\s-]) from version (\d+)")]
        public async Task WhenIReadStreamFromVersion(string streamId, ulong version)
        {
            this.Context.StreamId = streamId;

            var response = await this.Context.EventStore.ReadStream(streamId, new ReadStreamOptions { FromVersion = version });

            this.Context.Stream = response.Stream;
            this.Context.Response = response;
        }

        [When(@"I read stream ([^\s-]) from version (\d+) ignoring snapshots")]
        public async Task WhenIReadStreamFromVersionIgnoringSnapshots(string streamId, ulong version)
        {
            this.Context.StreamId = streamId;

            var response = await this.Context.EventStore.ReadStream(streamId, new ReadStreamOptions { FromVersion = version, IgnoreSnapshots = true });

            this.Context.Stream = response.Stream;
            this.Context.Response = response;
        }

        [When(@"I read stream ([^\s-]) from version (\d+) to version (\d+)")]
        public async Task WhenIReadStreamFromVersionToVersion(string streamId, ulong fromVersion, ulong toVersion)
        {
            this.Context.StreamId = streamId;

            var response = await this.Context.EventStore.ReadStream(streamId, new ReadStreamOptions { FromVersion = fromVersion, ToVersion = toVersion });

            this.Context.Stream = response.Stream;
            this.Context.Response = response;
        }

        [When(@"I read stream ([^\s-]) from version (\d+) to version (\d+) ignoring snapshots")]
        public async Task WhenIReadStreamFromVersionToVersionIgnoringSnapshots(string streamId, ulong fromVersion, ulong toVersion)
        {
            this.Context.StreamId = streamId;

            var response = await this.Context.EventStore.ReadStream(streamId, new ReadStreamOptions { FromVersion = fromVersion, ToVersion = toVersion, IgnoreSnapshots = true });

            this.Context.Stream = response.Stream;
            this.Context.Response = response;
        }

        [When(@"I read stream ([^\s-]) ignoring snapshots")]
        public async Task WhenIReadStreamIgnoringSnapshots(string streamId)
        {
            this.Context.StreamId = streamId;

            var response = await this.Context.EventStore.ReadStream(streamId, new ReadStreamOptions { IgnoreSnapshots = true });

            this.Context.Stream = response.Stream;
            this.Context.Response = response;
        }

        [Then(@"the non-existing stream is returned")]
        public void ThenTheNon_ExistingStreamIsReturned()
        {
            var stream = this.Context.Stream;

            Assert.That(stream.HasValue, Is.False);
            Assert.That((this.Context.Response as StreamResponse).SoftDeleted, Is.False);
        }

        [Then(@"the non-existing, soft-deleted stream is returned")]
        public void ThenTheNon_ExistingSoft_DeletedStreamIsReturned()
        {
            var stream = this.Context.Stream;

            Assert.That(stream.HasValue, Is.False);
            Assert.That((this.Context.Response as StreamResponse).SoftDeleted);
        }

        [Then(@"the stream ([^\s-]) in version (\d+) is returned")]
        public void ThenTheStreamInVersionIsReturned(string streamId, ulong version)
        {
            var stream = this.Context.Stream;

            Assert.That(stream.HasValue);
            Assert.That(stream.Value.StreamId, Is.EqualTo(streamId));
            Assert.That(stream.Value.Version, Is.EqualTo(version));
            Assert.That(stream.Value.Metadata, Is.Null);
        }

        [Then(@"the stream ([^\s-]) with metadata in version (\d+) is returned")]
        public void ThenTheStreamWithMetadataInVersionIsReturned(string streamId, ulong version)
        {
            var stream = this.Context.Stream;

            Assert.That(stream.HasValue);
            Assert.That(stream.Value.StreamId, Is.EqualTo(streamId));
            Assert.That(stream.Value.Version, Is.EqualTo(version));
            Assert.That(JToken.FromObject(stream.Value.Metadata), Is.EqualTo(JToken.FromObject(this.Context.HeaderMetadata)));
        }

        [Then(@"no snapshot is returned")]
        public void ThenNoSnapshotIsReturned()
        {
            var stream = this.Context.Stream;

            Assert.That(stream.HasValue);
            Assert.That(stream.Value.Snapshot.HasValue, Is.False);
        }

        [Then(@"a snapshot for version (\d+) is returned")]
        public void ThenASnapshotForVersionIsReturned(ulong version)
        {
            var stream = this.Context.Stream;

            Assert.That(stream.HasValue);
            Assert.That(stream.Value.Snapshot.HasValue);
            Assert.That(stream.Value.Snapshot.Value.Version, Is.EqualTo(version));
            Assert.That(JToken.FromObject(stream.Value.Snapshot.Value.Data), Is.EqualTo(JToken.FromObject(this.Context.Snapshot)));
            Assert.That(stream.Value.Snapshot.Value.Metadata, Is.Null);
        }

        [Then(@"a snapshot with metadata for version (\d+) is returned")]
        public void ThenASnapshotWithMetadataForVersionIsReturned(ulong version)
        {
            var stream = this.Context.Stream;

            Assert.That(stream.HasValue);
            Assert.That(stream.Value.Snapshot.HasValue);
            Assert.That(stream.Value.Snapshot.Value.Version, Is.EqualTo(version));
            Assert.That(JToken.FromObject(stream.Value.Snapshot.Value.Data), Is.EqualTo(JToken.FromObject(this.Context.Snapshot)));
            Assert.That(stream.Value.Snapshot.Value.Metadata, Is.Not.Null);
            Assert.That(JToken.FromObject(stream.Value.Snapshot.Value.Metadata), Is.EqualTo(JToken.FromObject(this.Context.SnapshotMetadata)));
        }

        [Then(@"no events are returned")]
        public void ThenNoEventsAreReturned()
        {
            var stream = this.Context.Stream;

            Assert.That(stream.HasValue);
            Assert.That(stream.Value.Events, Is.Empty);
        }

        [Then(@"events from version (\d+) to (\d+) are returned")]
        public async Task ThenEventsFromVersionToAreReturned(ulong fromVersion, ulong toVersion)
        {
            var stream = this.Context.Stream;
            var allDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, stream.Value.StreamId, Documents.DocumentType.Event);
            var eventDocuments = allDocuments.ToDictionary(x => x.Version);

            Assert.That(stream.HasValue);
            Assert.That(stream.Value.Events, Is.Not.Empty);
            Assert.That(stream.Value.Events.Length, Is.EqualTo(toVersion - fromVersion + 1));

            for(ulong version = fromVersion, index = 0; version <= toVersion; ++version, ++index)
            {
                var @event = stream.Value.Events[index];

                Assert.That(@event.Version, Is.EqualTo(version));
                Assert.That(eventDocuments.ContainsKey(version));

                var eventDocument = eventDocuments[version];

                Assert.That(eventDocument.Metadata, Is.EqualTo(JToken.FromObject(@event.Metadata)));
            }
        }
    }
}
