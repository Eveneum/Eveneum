using System.Threading.Tasks;
using NUnit.Framework;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastrature;
using Eveneum.Documents;

namespace Eveneum.Tests
{
    [Binding]
    public class DeletingStreamSteps
    {
        private readonly CosmosDbContext Context;

        DeletingStreamSteps(CosmosDbContext context)
        {
            this.Context = context;
        }

        [When(@"I delete stream ([^\s-]) in expected version (\d+)")]
        public async Task WhenIDeleteStreamInExpectedVersion(string streamId, ulong expectedVersion)
        {
            ScenarioContext.Current.SetStreamId(streamId);
            ScenarioContext.Current.SetExistingDocuments(await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection));

            await this.Context.EventStore.DeleteStream(ScenarioContext.Current.GetStreamId(), expectedVersion);
        }

        [Then(@"the header is soft-deleted")]
        public async Task ThenTheHeaderIsSoft_Deleted()
        {
            var headerDocumentResponse = await this.Context.Client.GetDatabase(this.Context.Database).GetContainer(this.Context.Collection).ReadItemAsync<EveneumDocument>(ScenarioContext.Current.GetStreamId(), this.Context.PartitionKey);

            Assert.IsNotNull(headerDocumentResponse.Resource);

            var headerDocument = headerDocumentResponse.Resource;

            Assert.IsTrue(headerDocument.Deleted);
        }

        [Then(@"the header is hard-deleted")]
        public async Task ThenTheHeaderIsHard_Deleted()
        {
            var documents = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Collection, ScenarioContext.Current.GetStreamId(), this.Context.PartitionKey, DocumentType.Header);

            Assert.IsEmpty(documents);
        }

        [Then(@"all events are soft-deleted")]
        public async Task ThenAllEventsAreSoft_Deleted()
        {
            var streamId = ScenarioContext.Current.GetStreamId();
            var eventDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Collection, streamId, this.Context.PartitionKey, DocumentType.Event);

            foreach (var eventDocument in eventDocuments)
                Assert.IsTrue(eventDocument.Deleted);
        }

        [Then(@"all events are hard-deleted")]
        public async Task ThenAllEventsAreHard_Deleted()
        {
            var streamId = ScenarioContext.Current.GetStreamId();
            var eventDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Collection, streamId, this.Context.PartitionKey, DocumentType.Event);

            Assert.IsEmpty(eventDocuments);
        }

        [Then(@"all snapshots are soft-deleted")]
        public async Task ThenAllSnapshotsAreSoft_Deleted()
        {
            var streamId = ScenarioContext.Current.GetStreamId();
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Collection, streamId, this.Context.PartitionKey, DocumentType.Snapshot);

            foreach (var snapshotDocument in snapshotDocuments)
                Assert.IsTrue(snapshotDocument.Deleted);
        }

        [Then(@"all snapshots are hard-deleted")]
        public async Task ThenAllSnapshotsAreHard_Deleted()
        {
            var streamId = ScenarioContext.Current.GetStreamId();
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Collection, streamId, this.Context.PartitionKey, DocumentType.Snapshot);

            Assert.IsEmpty(snapshotDocuments);
        }

        [Then(@"stream ([^\s-]) is not soft-deleted")]
        public async Task ThenStreamIsNotSoft_Deleted(string streamId)
        {
            var documents = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Collection, streamId, this.Context.PartitionKey);

            foreach (var document in documents)
                Assert.IsFalse(document.Deleted);
        }

        [Then(@"stream ([^\s-]) is not hard-deleted")]
        public async Task ThenStreamIsNotHard_Deleted(string streamId)
        {
            var documents = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Collection, streamId, this.Context.PartitionKey);

            Assert.IsNotEmpty(documents);
        }
    }
}
