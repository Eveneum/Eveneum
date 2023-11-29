using System.Threading.Tasks;
using NUnit.Framework;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastructure;
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
            this.Context.StreamId = streamId;
            this.Context.ExistingDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Container);

            var response = await this.Context.EventStore.DeleteStream(streamId, expectedVersion);

            this.Context.Response = response;
        }

        [Then(@"the header is soft-deleted")]
        public async Task ThenTheHeaderIsSoft_Deleted()
        {
            await ThenTheHeaderIsSoft_Deleted(null);
        }

        [Then(@"the header is soft-deleted with TTL set to (\d+) seconds")]
        public async Task ThenTheHeaderIsSoft_Deleted(int? ttl)
        {
            var documents = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Header);
            Assert.That(documents.Count, Is.EqualTo(1));

            var headerDocument = documents[0];

            Assert.That(headerDocument.Deleted);
            Assert.That(headerDocument.TimeToLive, Is.EqualTo(ttl));
        }

        [Then(@"the header is hard-deleted")]
        public async Task ThenTheHeaderIsHard_Deleted()
        {
            var documents = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Header);

            Assert.That(documents, Is.Empty);
        }

        [Then(@"all events are soft-deleted")]
        public async Task ThenAllEventsAreSoft_Deleted()
        {
            await ThenAllEventsAreSoft_Deleted(null);
        }

        [Then(@"all events are soft-deleted with TTL set to (\d+) seconds")]
        public async Task ThenAllEventsAreSoft_Deleted(int? ttl)
        {
            var streamId = this.Context.StreamId;
            var eventDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId, DocumentType.Event);

            foreach (var eventDocument in eventDocuments)
            {
                Assert.That(eventDocument.Deleted);
                Assert.That(eventDocument.TimeToLive, Is.EqualTo(ttl));
            }
        }

        [Then(@"all events are hard-deleted")]
        public async Task ThenAllEventsAreHard_Deleted()
        {
            var streamId = this.Context.StreamId;
            var eventDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId, DocumentType.Event);

            Assert.That(eventDocuments, Is.Empty);
        }

        [Then(@"all snapshots are soft-deleted")]
        public async Task ThenAllSnapshotsAreSoft_Deleted()
        {
            await ThenAllSnapshotsAreSoft_Deleted(null);
        }

        [Then(@"all snapshots are soft-deleted with TTL set to (\d+) seconds")]
        public async Task ThenAllSnapshotsAreSoft_Deleted(int? ttl)
        {
            var streamId = this.Context.StreamId;
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId, DocumentType.Snapshot);

            foreach (var snapshotDocument in snapshotDocuments)
            {
                Assert.That(snapshotDocument.Deleted);
                Assert.That(snapshotDocument.TimeToLive, Is.EqualTo(ttl));
            }
        }


        [Then(@"all snapshots are hard-deleted")]
        public async Task ThenAllSnapshotsAreHard_Deleted()
        {
            var streamId = this.Context.StreamId;
            var snapshotDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId, DocumentType.Snapshot);

            Assert.That(snapshotDocuments, Is.Empty);
        }

        [Then(@"stream ([^\s-]) is not soft-deleted")]
        public async Task ThenStreamIsNotSoft_Deleted(string streamId)
        {
            var documents = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId);

            foreach (var document in documents)
                Assert.That(document.Deleted, Is.False);
        }

        [Then(@"stream ([^\s-]) is not hard-deleted")]
        public async Task ThenStreamIsNotHard_Deleted(string streamId)
        {
            var documents = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId);

            Assert.That(documents, Is.Not.Empty);
        }
    }
}
