using System;
using System.Threading.Tasks;
using NUnit.Framework;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastrature;
using Eveneum.Documents;
using Microsoft.Azure.Documents.Client;
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
            await this.Context.EventStore.WriteSnapshot(ScenarioContext.Current.GetStreamId(), version, TestSetup.GetSnapshot());
        }

        [When(@"I create snapshot for stream (.*) in version (\d+)")]
        public async Task WhenICreateSnapshotForStreamInVersion(string streamId, ulong version)
        {
            ScenarioContext.Current.SetSnapshot(TestSetup.GetSnapshot());

            await this.Context.EventStore.WriteSnapshot(streamId, version, ScenarioContext.Current.GetSnapshot(), ScenarioContext.Current.GetSnapshotMetadata());
        }

        [When(@"I create snapshot with metadata for stream (.*) in version (\d+)")]
        public async Task WhenICreateSnapshotWithMetadataForStreamInVersion(string streamId, ulong version)
        {
            ScenarioContext.Current.SetSnapshotMetadata(TestSetup.GetMetadata());

            await WhenICreateSnapshotForStreamInVersion(streamId, version);
        }

        [When(@"I create snapshot for stream (.*) in version (\d+) and delete older snapshots")]
        public async Task WhenICreateSnapshotForStreamInVersionAndDeleteOlderSnapshots(string streamId, ulong version)
        {
            ScenarioContext.Current.SetSnapshot(TestSetup.GetSnapshot());

            await this.Context.EventStore.WriteSnapshot(streamId, version, ScenarioContext.Current.GetSnapshot(), deleteOlderSnapshots: true);
        }

        [When(@"I delete snapshots older than version (\d+) from stream (.*)")]
        public async Task WhenIDeleteSnapshotsOlderThanVersionFromStream(ulong version, string streamId)
        {
            await this.Context.EventStore.DeleteSnapshots(streamId, version);
        }

        [Then(@"the snapshot for version (\d+) is persisted")]
        public async Task ThenTheSnapshotForVersionIsPersisted(ulong version)
        {
            var streamId = ScenarioContext.Current.GetStreamId();
            var snapshot = ScenarioContext.Current.GetSnapshot();

            var snapshotUri = UriFactory.CreateDocumentUri(this.Context.Database, this.Context.Collection, SnapshotDocument.GenerateId(streamId, version));
            var snapshotDocumentResponse = await this.Context.Client.ReadDocumentAsync<SnapshotDocument>(snapshotUri, new RequestOptions { PartitionKey = this.Context.PartitionKey });

            Assert.IsNotNull(snapshotDocumentResponse.Document);

            var snapshotDocument = snapshotDocumentResponse.Document;
            var snapshotMetadata = ScenarioContext.Current.GetSnapshotMetadata();

            Assert.AreEqual(this.Context.Partition, snapshotDocument.Partition);
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
            var streamId = ScenarioContext.Current.GetStreamId();
            var currentDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection);

            var olderSnapshotDocuments = currentDocuments
                .OfType<SnapshotDocument>()
                .Where(x => x.Partition == this.Context.Partition && x.StreamId == streamId)
                .Where(x => x.Version < version)
                .ToList();

            foreach(var olderSnapshotDocument in olderSnapshotDocuments)
            {
                Assert.IsTrue(olderSnapshotDocument.Deleted);
            }
        }

        [Then(@"snapshots (\d+) and newer are not soft-deleted")]
        public async Task ThenSnapshotsAndNewerAreNotSoft_Deleted(ulong version)
        {
            var streamId = ScenarioContext.Current.GetStreamId();
            var currentDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection);

            var olderSnapshotDocuments = currentDocuments
                .OfType<SnapshotDocument>()
                .Where(x => x.Partition == this.Context.Partition && x.StreamId == streamId)
                .Where(x => x.Version >= version)
                .ToList();

            foreach (var olderSnapshotDocument in olderSnapshotDocuments)
            {
                Assert.IsFalse(olderSnapshotDocument.Deleted);
            }
        }
    }
}
