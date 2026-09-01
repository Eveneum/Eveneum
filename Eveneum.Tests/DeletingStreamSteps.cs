using Eveneum.Documents;
using Eveneum.Tests.Infrastructure;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Reqnroll;

namespace Eveneum.Tests
{
    [Binding]
    public class DeletingStreamSteps(NewtonsoftCosmosDbContext newtonsoftContext, SystemTextJsonCosmosDbContext stjContext)
    {
        private readonly IReadOnlyCollection<CosmosDbContext> Contexts = [newtonsoftContext, stjContext];

        [When("I delete stream {word} in expected version {int}")]
        public async Task WhenIDeleteStreamInExpectedVersion(string streamId, ulong expectedVersion)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                x.StreamId = streamId;
                var existingDocuments = await CosmosSetup.QueryAllDocuments(x.Client, x.Database, x.Container);
                x.ExistingDocuments = existingDocuments;
                x.Response = await x.EventStore.DeleteStream(streamId, expectedVersion);
            }));
        }

        [Then(@"the header is soft-deleted")]
        public async Task ThenTheHeaderIsSoft_Deleted()
        {
            await ThenTheHeaderIsSoft_Deleted(null);
        }

        [Then("the header is soft-deleted with TTL set to {int} seconds")]
        public async Task ThenTheHeaderIsSoft_Deleted(int? ttl)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var documents = await CosmosSetup.QueryAllDocumentsInStream(x.Client, x.Database, x.Container, x.StreamId, DocumentType.Header);

                Assert.That(documents, Has.Count.EqualTo(1));
                Assert.That(documents[0].Deleted);
                Assert.That(documents[0].TimeToLive, Is.EqualTo(ttl));
            }));
        }

        [Then(@"the header is hard-deleted")]
        public Task ThenTheHeaderIsHard_Deleted() => AllDocumentsOfTypeAreHardDeleted(DocumentType.Header);

        [Then(@"all events are soft-deleted")]
        public async Task ThenAllEventsAreSoft_Deleted()
        {
            await ThenAllEventsAreSoft_Deleted(null);
        }

        [Then("all events are soft-deleted with TTL set to {int} seconds")]
        public async Task ThenAllEventsAreSoft_Deleted(int? ttl)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var documents = await CosmosSetup.QueryAllDocumentsInStream(x.Client, x.Database, x.Container, x.StreamId, DocumentType.Event);

                foreach (var eventDocument in documents)
                {
                    Assert.That(eventDocument.Deleted);
                    Assert.That(eventDocument.TimeToLive, Is.EqualTo(ttl));
                }
            }));
        }

        [Then(@"all events are hard-deleted")]
        public Task ThenAllEventsAreHard_Deleted() => AllDocumentsOfTypeAreHardDeleted(DocumentType.Event);

        [Then(@"all snapshots are hard-deleted")]
        public Task ThenAllSnapshotsAreHard_Deleted() => AllDocumentsOfTypeAreHardDeleted(DocumentType.Snapshot);

        [Then("stream {word} is not soft-deleted")]
        public async Task ThenStreamIsNotSoft_Deleted(string streamId)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var documents = await CosmosSetup.QueryAllDocumentsInStream(x.Client, x.Database, x.Container, streamId, DocumentType.Event);

                foreach (var eventDocument in documents)
                {
                    Assert.That(eventDocument.Deleted, Is.False);
                }
            }));
        }

        [Then("stream {word} is not hard-deleted")]
        public async Task ThenStreamIsNotHard_Deleted(string streamId)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var documents = await CosmosSetup.QueryAllDocumentsInStream(x.Client, x.Database, x.Container, streamId);

                Assert.That(documents, Is.Not.Empty);
            }));
        }

        [Then(@"all snapshots are soft-deleted")]
        public async Task ThenAllSnapshotsAreSoft_Deleted()
        {
            await ThenAllSnapshotsAreSoft_Deleted(null);
        }

        [Then("all snapshots are soft-deleted with TTL set to {int} seconds")]
        public async Task ThenAllSnapshotsAreSoft_Deleted(int? ttl)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var documents = await CosmosSetup.QueryAllDocumentsInStream(x.Client, x.Database, x.Container, x.StreamId, DocumentType.Snapshot);

                foreach (var snapshotDocument in documents)
                {
                    Assert.That(snapshotDocument.Deleted);
                    Assert.That(snapshotDocument.TimeToLive, Is.EqualTo(ttl));
                }
            }));
        }

        private async Task AllDocumentsOfTypeAreHardDeleted(DocumentType documentType)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var documents = await CosmosSetup.QueryAllDocumentsInStream(x.Client, x.Database, x.Container, x.StreamId, documentType);

                Assert.That(documents, Is.Empty);
            }));
        }
    }
}
