using System.Threading.Tasks;
using NUnit.Framework;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastrature;
using Eveneum.Documents;
using Microsoft.Azure.Documents.Client;
using System.Linq;

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

        [When(@"I delete stream (.*) in expected version (\d+)")]
        public async Task WhenIDeleteStreamInExpectedVersion(string streamId, ulong expectedVersion)
        {
            ScenarioContext.Current.SetStreamId(streamId);
            ScenarioContext.Current.SetExistingDocuments(await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection));

            await this.Context.EventStore.DeleteStream(ScenarioContext.Current.GetStreamId(), expectedVersion);
        }

        [Then(@"the header is soft-deleted")]
        public async Task ThenTheHeaderIsSoft_Deleted()
        {
            var headerDocumentResponse = await this.Context.Client.ReadDocumentAsync<HeaderDocument>(UriFactory.CreateDocumentUri(this.Context.Database, this.Context.Collection, ScenarioContext.Current.GetStreamId()), new RequestOptions { PartitionKey = this.Context.PartitionKey });

            Assert.IsNotNull(headerDocumentResponse.Document);

            var headerDocument = headerDocumentResponse.Document;

            Assert.IsTrue(headerDocument.Deleted);
        }

        [Then(@"all events are soft-deleted")]
        public async Task ThenAllEventsAreSoft_Deleted()
        {
            var streamId = ScenarioContext.Current.GetStreamId();
            var currentDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection);

            var eventDocuments = currentDocuments
                .OfType<EventDocument>()
                .Where(x => x.Partition == this.Context.Partition && x.StreamId == streamId)
                .ToList();

            foreach (var eventDocument in eventDocuments)
                Assert.IsTrue(eventDocument.Deleted);
        }

        [Then(@"all snapshots are soft-deleted")]
        public async Task ThenAllSnapshotsAreSoft_Deleted()
        {
            var streamId = ScenarioContext.Current.GetStreamId();
            var currentDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection);

            var snapshotDocuments = currentDocuments
                .OfType<SnapshotDocument>()
                .Where(x => x.Partition == this.Context.Partition && x.StreamId == streamId)
                .ToList();

            foreach (var snapshotDocument in snapshotDocuments)
                Assert.IsTrue(snapshotDocument.Deleted);
        }

        [Then(@"stream (.*) is not soft-deleted")]
        public async Task ThenStreamIsNotSoft_Deleted(string streamId)
        {
            var currentDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection);

            var documents = currentDocuments
                .Where(x => x.Partition == this.Context.Partition && x.StreamId == streamId)
                .ToList();

            foreach (var document in documents)
                Assert.IsFalse(document.Deleted);
        }
    }
}
