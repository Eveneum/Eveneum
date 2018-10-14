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

        [When(@"I create snapshot for stream (.*) in version (\d+)")]
        public async Task WhenICreateSnapshotForStreamInVersion(string streamId, ulong version)
        {
            ScenarioContext.Current.SetSnapshot(TestSetup.GetSnapshot());

            await this.Context.EventStore.WriteSnapshot(streamId, version, ScenarioContext.Current.GetSnapshot());
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

            Assert.AreEqual(this.Context.Partition, snapshotDocument.Partition);
            Assert.AreEqual(DocumentType.Snapshot, snapshotDocument.DocumentType);
            Assert.AreEqual(streamId, snapshotDocument.StreamId);
            Assert.AreEqual(version, snapshotDocument.Version);
            Assert.AreEqual(version + EveneumDocument.GetOrderingFraction(DocumentType.Snapshot), snapshotDocument.SortOrder);
            Assert.IsNull(snapshotDocument.MetadataType);
            Assert.IsFalse(snapshotDocument.Metadata.HasValues);
            Assert.AreEqual(snapshot.GetType().AssemblyQualifiedName, snapshotDocument.BodyType);
            Assert.AreEqual(JToken.FromObject(snapshot), snapshotDocument.Body);
            Assert.False(snapshotDocument.Deleted);
            Assert.IsNotNull(snapshotDocument.ETag);
        }
    }
}
