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
    }
}
