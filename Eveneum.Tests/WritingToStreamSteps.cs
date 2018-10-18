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
    public class WritingToStreamSteps
    {
        private readonly CosmosDbContext Context;

        WritingToStreamSteps(CosmosDbContext context)
        {
            this.Context = context;
        }

        [When(@"I write a new stream ([^\s-]) with (\d+) events")]
        public async Task WhenIWriteNewStreamWithEvents(string streamId, int events)
        {
            ScenarioContext.Current.SetStreamId(streamId);
            ScenarioContext.Current.SetNewEvents(TestSetup.GetEvents(events));
            ScenarioContext.Current.SetExistingDocuments(await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection));

            await this.Context.EventStore.WriteToStream(ScenarioContext.Current.GetStreamId(), ScenarioContext.Current.GetNewEvents(), metadata: ScenarioContext.Current.GetHeaderMetadata());
        }

        [When(@"I write a new stream ([^\s-]) with metadata and (\d+) events")]
        public async Task WhenIWriteNewStreamWithMetadataAndNoEvents(string streamId, int events)
        {
            ScenarioContext.Current.SetHeaderMetadata(TestSetup.GetMetadata());

            await WhenIWriteNewStreamWithEvents(streamId, events);
        }

        [When(@"I append (\d+) events to stream ([^\s-]) in expected version (\d+)")]
        public async Task WhenIAppendEventsToStreamInExpectedVersion(int events, string streamId, ushort expectedVersion)
        {
            ScenarioContext.Current.SetStreamId(streamId);
            ScenarioContext.Current.SetNewEvents(TestSetup.GetEvents(events, expectedVersion + 1));
            ScenarioContext.Current.SetExistingDocuments(await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection));

            await this.Context.EventStore.WriteToStream(ScenarioContext.Current.GetStreamId(), ScenarioContext.Current.GetNewEvents(), expectedVersion, metadata: ScenarioContext.Current.GetHeaderMetadata());
        }

        [Then(@"the header version (\d+) with no metadata is persisted")]
        public async Task ThenTheHeaderVersionWithNoMetadataIsPersisted(ulong version)
        {
            var headerDocumentResponse = await this.Context.Client.ReadDocumentAsync<HeaderDocument>(UriFactory.CreateDocumentUri(this.Context.Database, this.Context.Collection, ScenarioContext.Current.GetStreamId()), new RequestOptions { PartitionKey = this.Context.PartitionKey });

            Assert.IsNotNull(headerDocumentResponse.Document);

            var headerDocument = headerDocumentResponse.Document;

            Assert.AreEqual(this.Context.Partition, headerDocument.Partition);
            Assert.AreEqual(DocumentType.Header, headerDocument.DocumentType);
            Assert.AreEqual(ScenarioContext.Current.GetStreamId(), headerDocument.StreamId);
            Assert.AreEqual(version, headerDocument.Version);
            Assert.AreEqual(version + EveneumDocument.GetOrderingFraction(DocumentType.Header), headerDocument.SortOrder);
            Assert.IsNull(headerDocument.MetadataType);
            Assert.IsFalse(headerDocument.Metadata.HasValues);
            Assert.NotNull(headerDocument.ETag);
            Assert.False(headerDocument.Deleted);
        }

        [Then(@"the header version (\d+) with metadata is persisted")]
        public async Task ThenTheHeaderVersionWithMetadataIsPersisted(ulong version)
        {
            var headerDocumentResponse = await this.Context.Client.ReadDocumentAsync<HeaderDocument>(UriFactory.CreateDocumentUri(this.Context.Database, this.Context.Collection, ScenarioContext.Current.GetStreamId()), new RequestOptions { PartitionKey = this.Context.PartitionKey });

            Assert.IsNotNull(headerDocumentResponse.Document);

            var headerDocument = headerDocumentResponse.Document;

            Assert.AreEqual(this.Context.Partition, headerDocument.Partition);
            Assert.AreEqual(DocumentType.Header, headerDocument.DocumentType);
            Assert.AreEqual(ScenarioContext.Current.GetStreamId(), headerDocument.StreamId);
            Assert.AreEqual(version, headerDocument.Version);
            Assert.AreEqual(version + EveneumDocument.GetOrderingFraction(DocumentType.Header), headerDocument.SortOrder);
            Assert.AreEqual(typeof(SampleMetadata).AssemblyQualifiedName, headerDocument.MetadataType);
            Assert.NotNull(headerDocument.Metadata);
            Assert.AreEqual(JToken.FromObject(ScenarioContext.Current.GetHeaderMetadata()), headerDocument.Metadata);
            Assert.NotNull(headerDocument.ETag);
            Assert.False(headerDocument.Deleted);
        }

        [Then(@"the action fails as stream ([^\s-]) already exists")]
        public void ThenTheActionFailsAsStreamAlreadyExists(string streamId)
        {
            Assert.NotNull(ScenarioContext.Current.TestError);
            Assert.IsInstanceOf<StreamAlreadyExistsException>(ScenarioContext.Current.TestError);

            var exception = ScenarioContext.Current.TestError as StreamAlreadyExistsException;
            Assert.AreEqual(streamId, exception.StreamId);
        }

        [Then(@"the action fails as stream ([^\s-]) doesn't exist")]
        public void ThenTheActionFailsAsStreamDoesnTExist(string streamId)
        {
            Assert.NotNull(ScenarioContext.Current.TestError);
            Assert.IsInstanceOf<StreamNotFoundException>(ScenarioContext.Current.TestError);

            var exception = ScenarioContext.Current.TestError as StreamNotFoundException;
            Assert.AreEqual(streamId, exception.StreamId);
        }

        [Then(@"the action fails as expected version (\d+) doesn't match the current version (\d+) of stream ([^\s-])")]
        public void ThenTheActionFailsAsExpectedVersionDoesnTMatchTheCurrentVersionOfStream(ulong expectedVersion, ulong currentVersion, string streamId)
        {
            Assert.NotNull(ScenarioContext.Current.TestError);
            Assert.IsInstanceOf<OptimisticConcurrencyException>(ScenarioContext.Current.TestError);

            var exception = ScenarioContext.Current.TestError as OptimisticConcurrencyException;
            Assert.AreEqual(streamId, exception.StreamId);
            Assert.AreEqual(expectedVersion, exception.ExpectedVersion);
            Assert.AreEqual(currentVersion, exception.ActualVersion);
        }

        [Then(@"no events are appended")]
        public async Task ThenNoEventsAreAppended()
        {
            var streamId = ScenarioContext.Current.GetStreamId();
            var currentDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection);
            var existingDocumentIds = ScenarioContext.Current.GetExistingDocuments().Select(x => x.Id);

            var newEventDocuments = currentDocuments
                .OfType<EventDocument>()
                .Where(x => x.Partition == this.Context.Partition && x.StreamId == streamId)
                .Where(x => !existingDocumentIds.Contains(x.Id));

            Assert.IsEmpty(newEventDocuments);
        }

        [Then(@"new events are appended")]
        public async Task ThenNewEventsAreAppended()
        {
            var streamId = ScenarioContext.Current.GetStreamId();
            var currentDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Collection);
            var existingDocumentIds = ScenarioContext.Current.GetExistingDocuments().Select(x => x.Id);

            var newEventDocuments = currentDocuments
                .OfType<EventDocument>()
                .Where(x => x.Partition == this.Context.Partition && x.StreamId == streamId)
                .Where(x => !existingDocumentIds.Contains(x.Id))
                .ToList();

            var newEvents = ScenarioContext.Current.GetNewEvents();

            Assert.AreEqual(newEventDocuments.Count, newEvents.Length);

            foreach (var newEvent in newEvents)
            {
                var eventDocument = newEventDocuments.Find(x => x.Partition == this.Context.Partition && x.Id == EventDocument.GenerateId(streamId, newEvent.Version));

                Assert.IsNotNull(eventDocument);
                Assert.AreEqual(DocumentType.Event, eventDocument.DocumentType);
                Assert.AreEqual(streamId, eventDocument.StreamId);
                Assert.AreEqual(newEvent.Body.GetType().AssemblyQualifiedName, eventDocument.BodyType);
                Assert.NotNull(eventDocument.Body);
                Assert.AreEqual(JToken.FromObject(newEvent.Body), eventDocument.Body);
                Assert.NotNull(eventDocument.ETag);
                Assert.False(eventDocument.Deleted);
            }
        }
    }
}
