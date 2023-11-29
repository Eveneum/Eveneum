using System.Threading.Tasks;
using NUnit.Framework;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastructure;
using Eveneum.Documents;
using Newtonsoft.Json.Linq;
using System.Linq;
using Eveneum.Snapshots;
using System.Threading;
using System;
using Eveneum.Serialization;

namespace Eveneum.Tests
{
    class CustomSnapshotWriter : ISnapshotWriter
    {
        public string StreamId { get; private set; }
        public ulong Version { get; private set; }
        public object Snapshot { get; private set; }
        public object Metadata { get; private set; }

        public Task<bool> CreateSnapshot(string streamId, ulong version, object snapshot, object metadata = null, CancellationToken cancellationToken = default)
        {
            this.StreamId = streamId;
            this.Version = version;
            this.Snapshot = snapshot;
            this.Metadata = metadata;

            Console.WriteLine("Custom snapshot created for stream {0} in version {1}", streamId, version);

            return Task.FromResult(true);
        }

        public Task DeleteSnapshots(string streamId, ulong olderThanVersion, CancellationToken cancellationToken = default)
        {
            this.StreamId = streamId;
            this.Version = olderThanVersion;
            
            Console.WriteLine("Custom snapshots deleted for stream {0} in version older than {1}", streamId, olderThanVersion);

            return Task.CompletedTask;
        }

        public Task<Snapshot> ReadSnapshot(string streamId, ulong version, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("Reading custom snapshot for stream {0} in version {1}", streamId, version);
            
            return Task.FromResult(new Snapshot(this.Snapshot, this.Metadata, this.Version));
        }
    }
    
    [Binding]
    public class SnapshotSteps
    {
        private readonly CosmosDbContext Context;

        SnapshotSteps(CosmosDbContext context)
        {
            this.Context = context;
        }

        [Given(@"an existing snapshot for version (\d+)")]
        public async Task GivenAnExistingSnapshotForVersion(ulong version)
        {
            this.Context.Snapshot = TestSetup.GetSnapshot();

            await this.Context.EventStore.CreateSnapshot(this.Context.StreamId, version, this.Context.Snapshot, this.Context.SnapshotMetadata);
        }

        [Given(@"an existing snapshot with metadata for version (\d+)")]
        public async Task GivenAnExistingSnapshotWithMetadataForVersion(ulong version)
        {
            this.Context.SnapshotMetadata = TestSetup.GetMetadata();

            await this.GivenAnExistingSnapshotForVersion(version);
        }

        [Given(@"an existing custom snapshot for version (\d+)")]
        public async Task GivenAnExistingCustomSnapshotForVersion(ulong version)
        {
            this.Context.Snapshot = TestSetup.GetSnapshot();
            this.Context.SnapshotWriterSnapshot = new SnapshotWriterSnapshot(typeof(CustomSnapshotWriter).AssemblyQualifiedName);

            await this.Context.EventStore.CreateSnapshot(this.Context.StreamId, version, this.Context.Snapshot, this.Context.SnapshotMetadata);
        }

        [Given(@"an existing custom snapshot with metadata for version (\d+)")]
        public async Task GivenAnExistingCustomSnapshotWithMetadataForVersion(ulong version)
        {
            this.Context.SnapshotMetadata = TestSetup.GetMetadata();

            await this.GivenAnExistingCustomSnapshotForVersion(version);
        }

        [Given(@"a custom Snapshot Writer")]
        public void GivenACustomSnapshotWriter()
        {
            this.Context.EventStoreOptions.SnapshotWriter = new CustomSnapshotWriter();
        }

        [When(@"I create snapshot for stream ([^\s-]) in version (\d+)")]
        public async Task WhenICreateSnapshotForStreamInVersion(string streamId, ulong version)
        {
            this.Context.Snapshot = TestSetup.GetSnapshot();

            var response = await this.Context.EventStore.CreateSnapshot(streamId, version, this.Context.Snapshot, this.Context.SnapshotMetadata);

            this.Context.Response = response;
        }

        [When(@"I create snapshot with metadata for stream ([^\s-]) in version (\d+)")]
        public async Task WhenICreateSnapshotWithMetadataForStreamInVersion(string streamId, ulong version)
        {
            this.Context.SnapshotMetadata = TestSetup.GetMetadata();

            await WhenICreateSnapshotForStreamInVersion(streamId, version);
        }

        [When(@"I create snapshot for stream ([^\s-]) in version (\d+) and delete older snapshots")]
        public async Task WhenICreateSnapshotForStreamInVersionAndDeleteOlderSnapshots(string streamId, ulong version)
        {
            this.Context.Snapshot = TestSetup.GetSnapshot();;

            var response = await this.Context.EventStore.CreateSnapshot(streamId, version, this.Context.Snapshot, deleteOlderSnapshots: true);

            this.Context.Response = response;
        }

        [When(@"I delete snapshots older than version (\d+) from stream ([^\s-])")]
        public async Task WhenIDeleteSnapshotsOlderThanVersionFromStream(ulong version, string streamId)
        {
            var response = await this.Context.EventStore.DeleteSnapshots(streamId, version);

            this.Context.Response = response;
        }

        [Then(@"the snapshot for version (\d+) is persisted")]
        public async Task ThenTheSnapshotForVersionIsPersisted(ulong version)
        {
            var streamId = this.Context.StreamId;
            var snapshot = this.Context.Snapshot;

            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);

            Assert.That(snapshotDocuments, Is.Not.Empty);

            var snapshotDocument = snapshotDocuments.Find(x => x.Version == version);
            Assert.That(snapshotDocument, Is.Not.Null);

            var snapshotMetadata = this.Context.SnapshotMetadata;

            Assert.That(snapshotDocument.DocumentType, Is.EqualTo(DocumentType.Snapshot));
            Assert.That(snapshotDocument.StreamId, Is.EqualTo(streamId));
            Assert.That(snapshotDocument.Version, Is.EqualTo(version));
            Assert.That(snapshotDocument.SortOrder, Is.EqualTo(version + EveneumDocument.GetOrderingFraction(DocumentType.Snapshot)));

            if (snapshotMetadata == null)
            {
                Assert.That(snapshotDocument.MetadataType, Is.Null);
                Assert.That(snapshotDocument.Metadata.HasValues, Is.False);
            }
            else
            {
                Assert.That(snapshotDocument.MetadataType, Is.EqualTo(snapshotMetadata.GetType().AssemblyQualifiedName));
                Assert.That(snapshotDocument.Metadata, Is.EqualTo(JToken.FromObject(snapshotMetadata)));
            }

            Assert.That(snapshotDocument.BodyType, Is.EqualTo(snapshot.GetType().AssemblyQualifiedName));
            Assert.That(snapshotDocument.Body, Is.EqualTo(JToken.FromObject(snapshot)));
            Assert.That(snapshotDocument.Deleted, Is.False);
            Assert.That(snapshotDocument.ETag, Is.Not.Null);
        }

        [Then(@"the Snapshot Writer snapshot for version (\d+) is persisted")]
        public async Task ThenTheSnapshotWriterSnapshotForVersionIsPersisted(ulong version)
        {
            var streamId = this.Context.StreamId;
            var snapshot = new SnapshotWriterSnapshot(typeof(CustomSnapshotWriter).AssemblyQualifiedName);

            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);

            Assert.That(snapshotDocuments, Is.Not.Empty);

            var snapshotDocument = snapshotDocuments.Find(x => x.Version == version);
            Assert.That(snapshotDocument, Is.Not.Null);

            Assert.That(snapshotDocument.DocumentType, Is.EqualTo(DocumentType.Snapshot));
            Assert.That(snapshotDocument.StreamId, Is.EqualTo(streamId));
            Assert.That(snapshotDocument.Version, Is.EqualTo(version));
            Assert.That(snapshotDocument.SortOrder, Is.EqualTo(version + EveneumDocument.GetOrderingFraction(DocumentType.Snapshot)));

            Assert.That(snapshotDocument.MetadataType, Is.Null);
            Assert.That(snapshotDocument.Metadata.HasValues, Is.False);

            Assert.That(snapshotDocument.BodyType, Is.EqualTo(PlatformTypeProvider.SnapshotWriterSnapshotTypeIdentifier));
            Assert.That(snapshotDocument.Body, Is.EqualTo(JToken.FromObject(snapshot)));
            Assert.That(snapshotDocument.Deleted, Is.False);
            Assert.That(snapshotDocument.ETag, Is.Not.Null);
        }

        [Then(@"the custom snapshot for version (\d+) is persisted")]
        public void ThenTheCustomSnapshotForVersionIsPersisted(ulong version)
        {
            var streamId = this.Context.StreamId;
            var snapshot = this.Context.Snapshot;
            var metadata = this.Context.SnapshotMetadata;

            var snapshotWriter = this.Context.EventStoreOptions.SnapshotWriter as CustomSnapshotWriter;

            Assert.That(snapshotWriter.StreamId, Is.EqualTo(streamId));
            Assert.That(snapshotWriter.Version, Is.EqualTo(version));
            Assert.That(snapshotWriter.Snapshot, Is.EqualTo(snapshot));
            Assert.That(snapshotWriter.Metadata, Is.EqualTo(metadata));
        }

        [Then(@"the snapshots older than (\d+) are soft-deleted")]
        public async Task ThenTheSnapshotsOlderThanAreSoft_Deleted(ulong version)
        {
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);
            var olderSnapshotDocuments = snapshotDocuments.Where(x => x.Version < version);

            foreach (var olderSnapshotDocument in olderSnapshotDocuments)
                Assert.That(olderSnapshotDocument.Deleted);
        }

        [Then(@"the snapshots older than (\d+) are hard-deleted")]
        public async Task ThenTheSnapshotsOlderThanAreHard_Deleted(ulong version)
        {
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);
            var olderSnapshotDocuments = snapshotDocuments.Where(x => x.Version < version);

            Assert.That(olderSnapshotDocuments, Is.Empty);
        }

        [Then(@"snapshots (\d+) and newer are not soft-deleted")]
        public async Task ThenSnapshotsAndNewerAreNotSoft_Deleted(ulong version)
        {
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);
            var newerSnapshotDocuments = snapshotDocuments.Where(x => x.Version >= version);

            foreach (var newerSnapshotDocument in newerSnapshotDocuments)
                Assert.That(newerSnapshotDocument.Deleted, Is.False);
        }

        [Then(@"snapshots (\d+) and newer are not hard-deleted")]
        public async Task ThenSnapshotsAndNewerAreNotHard_Deleted(ulong version)
        {
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);
            var newerSnapshotDocuments = snapshotDocuments.Where(x => x.Version >= version);

            Assert.That(newerSnapshotDocuments, Is.Not.Empty);
        }
    }
}
