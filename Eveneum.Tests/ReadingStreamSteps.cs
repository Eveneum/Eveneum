using Eveneum.Tests.Infrastructure;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Eveneum.Tests
{
    [Binding]
    public class ReadingStreamSteps(NewtonsoftCosmosDbContext newtonsoftContext, SystemTextJsonCosmosDbContext stjContext)
    {
        private readonly IReadOnlyCollection<CosmosDbContext> Contexts = [newtonsoftContext, stjContext];

        [When(@"I read stream ([^\s-])")]
        public Task WhenIReadStream(string streamId) => this.WhenIReadStream(streamId, null);

        [When(@"I read stream ([^\s-]) as of version (\d+)")]
        public Task WhenIReadStreamAsOfVersion(string streamId, ulong version) => this.WhenIReadStream(streamId, new ReadStreamOptions { ToVersion = version });

        [When(@"I read stream ([^\s-]) from version (\d+)")]
        public Task WhenIReadStreamFromVersion(string streamId, ulong version) => this.WhenIReadStream(streamId, new ReadStreamOptions { FromVersion = version });

        [When(@"I read stream ([^\s-]) from version (\d+) ignoring snapshots")]
        public Task WhenIReadStreamFromVersionIgnoringSnapshots(string streamId, ulong version) => this.WhenIReadStream(streamId, new ReadStreamOptions { FromVersion = version, IgnoreSnapshots = true });

        [When(@"I read stream ([^\s-]) from version (\d+) to version (\d+)")]
        public Task WhenIReadStreamFromVersionToVersion(string streamId, ulong fromVersion, ulong toVersion) => this.WhenIReadStream(streamId, new ReadStreamOptions { FromVersion = fromVersion, ToVersion = toVersion });

        [When(@"I read stream ([^\s-]) from version (\d+) to version (\d+) ignoring snapshots")]
        public Task WhenIReadStreamFromVersionToVersionIgnoringSnapshots(string streamId, ulong fromVersion, ulong toVersion) => this.WhenIReadStream(streamId, new ReadStreamOptions { FromVersion = fromVersion, ToVersion = toVersion, IgnoreSnapshots = true });

        [When(@"I read stream ([^\s-]) ignoring snapshots")]
        public Task WhenIReadStreamIgnoringSnapshots(string streamId) => this.WhenIReadStream(streamId, new ReadStreamOptions { IgnoreSnapshots = true });

        [Then(@"the non-existing stream is returned")]
        public void ThenTheNon_ExistingStreamIsReturned()
        {
            foreach (var context in this.Contexts)
            {
                Assert.That(context.Stream.HasValue, Is.False);
                Assert.That((context.Response as StreamResponse).SoftDeleted, Is.False);
            }
        }

        [Then(@"the non-existing, soft-deleted stream is returned")]
        public void ThenTheNon_ExistingSoft_DeletedStreamIsReturned()
        {
            foreach (var context in this.Contexts)
            {
                Assert.That(context.Stream.HasValue, Is.False);
                Assert.That((context.Response as StreamResponse).SoftDeleted);
            }
        }

        [Then(@"the stream ([^\s-]) in version (\d+) is returned")]
        public void ThenTheStreamInVersionIsReturned(string streamId, ulong version)
        {
            foreach (var context in this.Contexts)
            {
                Assert.That(context.Stream.HasValue);
                Assert.That(context.Stream.Value.StreamId, Is.EqualTo(streamId));
                Assert.That(context.Stream.Value.Version, Is.EqualTo(version));
                Assert.That(context.Stream.Value.Metadata, Is.Null);
            }
        }

        [Then(@"the stream ([^\s-]) with metadata in version (\d+) is returned")]
        public void ThenTheStreamWithMetadataInVersionIsReturned(string streamId, ulong version)
        {
            foreach (var context in this.Contexts)
            {
                Assert.That(context.Stream.HasValue);
                Assert.That(context.Stream.Value.StreamId, Is.EqualTo(streamId));
                Assert.That(context.Stream.Value.Version, Is.EqualTo(version));
                Assert.That(context.AreEqual(context.Stream.Value.Metadata, context.HeaderMetadata), Is.True);
            }
        }

        [Then(@"no snapshot is returned")]
        public void ThenNoSnapshotIsReturned()
        {
            foreach (var context in this.Contexts)
            {
                Assert.That(context.Stream.HasValue);
                Assert.That(context.Stream.Value.Snapshot.HasValue, Is.False);
            }
        }

        [Then(@"a snapshot for version (\d+) is returned")]
        public void ThenASnapshotForVersionIsReturned(ulong version)
        {
            foreach (var context in this.Contexts)
            {
                Assert.That(context.Stream.HasValue);
                Assert.That(context.Stream.Value.Snapshot.HasValue);
                Assert.That(context.Stream.Value.Snapshot.Value.Version, Is.EqualTo(version));
                Assert.That(context.AreEqual(context.Stream.Value.Snapshot.Value.Data, context.Snapshot), Is.True);
                Assert.That(context.Stream.Value.Snapshot.Value.Metadata, Is.Null);
            }
        }

        [Then(@"a snapshot with metadata for version (\d+) is returned")]
        public void ThenASnapshotWithMetadataForVersionIsReturned(ulong version)
        {
            foreach (var context in this.Contexts)
            {
                Assert.That(context.Stream.HasValue);
                Assert.That(context.Stream.Value.Snapshot.HasValue);
                Assert.That(context.Stream.Value.Snapshot.Value.Version, Is.EqualTo(version));
                Assert.That(context.AreEqual(context.Stream.Value.Snapshot.Value.Data, context.Snapshot), Is.True);
                Assert.That(context.Stream.Value.Snapshot.Value.Metadata, Is.Not.Null);
                Assert.That(context.AreEqual(context.Stream.Value.Snapshot.Value.Metadata, context.SnapshotMetadata), Is.True);
            }
        }

        [Then(@"no events are returned")]
        public void ThenNoEventsAreReturned()
        {
            foreach (var context in this.Contexts)
            {
                Assert.That(context.Stream.HasValue, Is.True);
                Assert.That(context.Stream.Value.Events, Is.Empty);
            }
        }

        [Then(@"events from version (\d+) to (\d+) are returned")]
        public async Task ThenEventsFromVersionToAreReturned(ulong fromVersion, ulong toVersion)
        {
            await Task.WhenAll(this.Contexts.Select(async context =>
            {
                var stream = context.Stream;
                var documents = await CosmosSetup.QueryAllDocumentsInStream(context.Client, context.Database, context.Container, stream.Value.StreamId, Documents.DocumentType.Event);
                var eventDocuments = documents.ToDictionary(x => x.Version);

                Assert.That(stream.HasValue);
                Assert.That(stream.Value.Events, Is.Not.Empty);
                Assert.That(stream.Value.Events.Length, Is.EqualTo(toVersion - fromVersion + 1));

                for (ulong version = fromVersion, index = 0; version <= toVersion; ++version, ++index)
                {
                    var @event = stream.Value.Events[index];
                    Assert.That(@event.Version, Is.EqualTo(version));
                    Assert.That(eventDocuments.ContainsKey(version));

                    var eventDocument = eventDocuments[version];
                    Assert.That(context.AreEqual(eventDocument.Metadata, @event.Metadata), Is.True);
                }
            }));
        }

        private async Task WhenIReadStream(string streamId, ReadStreamOptions options)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                x.StreamId = streamId;
                var response = await x.EventStore.ReadStream(streamId, options);
                x.Stream = response.Stream;
                x.Response = response;
            }));
        }
    }
}
