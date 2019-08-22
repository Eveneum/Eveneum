using System.Threading.Tasks;
using NUnit.Framework;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastrature;
using Eveneum.Documents;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Eveneum.Tests
{
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
            this.Context.Snapshot = TestSetup.GetSnapshot();;

            await this.Context.EventStore.CreateSnapshot(this.Context.StreamId, version, this.Context.Snapshot, this.Context.SnapshotMetadata);
        }

        [Given(@"an existing snapshot with metadata for version (\d+)")]
        public async Task GivenAnExistingSnapshotWithMetadataForVersion(ulong version)
        {
            this.Context.SnapshotMetadata = TestSetup.GetMetadata();

            await this.GivenAnExistingSnapshotForVersion(version);
        }

        [When(@"I create snapshot for stream ([^\s-]) in version (\d+)")]
        public async Task WhenICreateSnapshotForStreamInVersion(string streamId, ulong version)
        {
            this.Context.Snapshot = TestSetup.GetSnapshot();;

            await this.Context.EventStore.CreateSnapshot(streamId, version, this.Context.Snapshot, this.Context.SnapshotMetadata);
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

            await this.Context.EventStore.CreateSnapshot(streamId, version, this.Context.Snapshot, deleteOlderSnapshots: true);
        }

        [When(@"I delete snapshots older than version (\d+) from stream ([^\s-])")]
        public async Task WhenIDeleteSnapshotsOlderThanVersionFromStream(ulong version, string streamId)
        {
            await this.Context.EventStore.DeleteSnapshots(streamId, version);
        }

        [Then(@"the snapshot for version (\d+) is persisted")]
        public async Task ThenTheSnapshotForVersionIsPersisted(ulong version)
        {
            var streamId = this.Context.StreamId;
            var snapshot = this.Context.Snapshot;

            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);

            Assert.IsNotEmpty(snapshotDocuments);

            var snapshotDocument = snapshotDocuments.Find(x => x.Version == version);
            Assert.IsNotNull(snapshotDocument);

            var snapshotMetadata = this.Context.SnapshotMetadata;

            Assert.AreEqual(DocumentType.Snapshot, snapshotDocument.DocumentType);
            Assert.AreEqual(streamId, snapshotDocument.StreamId);
            Assert.AreEqual(version, snapshotDocument.Version);
            Assert.AreEqual(version + EveneumDocument.GetOrderingFraction(DocumentType.Snapshot), snapshotDocument.SortOrder);

            if (snapshotMetadata == null)
            {
                Assert.IsNull(snapshotDocument.MetadataType);
                Assert.IsFalse(snapshotDocument.Metadata.HasValues);
            }
            else
            {
                Assert.AreEqual(snapshotMetadata.GetType().AssemblyQualifiedName, snapshotDocument.MetadataType);
                Assert.AreEqual(JToken.FromObject(snapshotMetadata), snapshotDocument.Metadata);
            }

            Assert.AreEqual(snapshot.GetType().AssemblyQualifiedName, snapshotDocument.BodyType);
            Assert.AreEqual(JToken.FromObject(snapshot), snapshotDocument.Body);
            Assert.False(snapshotDocument.Deleted);
            Assert.IsNotNull(snapshotDocument.ETag);
        }

        [Then(@"the snapshots older than (\d+) are soft-deleted")]
        public async Task ThenTheSnapshotsOlderThanAreSoft_Deleted(ulong version)
        {
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);
            var olderSnapshotDocuments = snapshotDocuments.Where(x => x.Version < version);

            foreach (var olderSnapshotDocument in olderSnapshotDocuments)
                Assert.IsTrue(olderSnapshotDocument.Deleted);
        }

        [Then(@"the snapshots older than (\d+) are hard-deleted")]
        public async Task ThenTheSnapshotsOlderThanAreHard_Deleted(ulong version)
        {
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);
            var olderSnapshotDocuments = snapshotDocuments.Where(x => x.Version < version);

            Assert.IsEmpty(olderSnapshotDocuments);
        }

        [Then(@"snapshots (\d+) and newer are not soft-deleted")]
        public async Task ThenSnapshotsAndNewerAreNotSoft_Deleted(ulong version)
        {
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);
            var newerSnapshotDocuments = snapshotDocuments.Where(x => x.Version >= version);

            foreach (var newerSnapshotDocument in newerSnapshotDocuments)
                Assert.IsFalse(newerSnapshotDocument.Deleted);
        }

        [Then(@"snapshots (\d+) and newer are not hard-deleted")]
        public async Task ThenSnapshotsAndNewerAreNotHard_Deleted(ulong version)
        {
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Snapshot);
            var newerSnapshotDocuments = snapshotDocuments.Where(x => x.Version >= version);

            Assert.IsNotEmpty(newerSnapshotDocuments);
        }
    }
}
