using System.Threading.Tasks;
using NUnit.Framework;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastructure;
using Eveneum.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Eveneum.Serialization;

namespace Eveneum.Tests
{
    [Binding]
    public class WritingToStreamSteps
    {
        private readonly CosmosDbContext Context;
        private readonly ScenarioContext ScenarioContext;

        WritingToStreamSteps(CosmosDbContext context, ScenarioContext scenarioContext)
        {
            this.Context = context;
            this.ScenarioContext = scenarioContext;
        }

        [When(@"I write a new stream ([^\s-]) with (\d+) events")]
        public async Task WhenIWriteNewStreamWithEvents(string streamId, int events)
        {
            this.Context.StreamId = streamId;
            this.Context.NewEvents = TestSetup.GetEvents(events);
            this.Context.ExistingDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Container);

            var response = await this.Context.EventStore.WriteToStream(this.Context.StreamId, this.Context.NewEvents, metadata: this.Context.HeaderMetadata);

            this.Context.Response = response;
        }

        [When(@"I write a new stream ([^\s-]) with metadata and (\d+) events")]
        public async Task WhenIWriteNewStreamWithMetadataAndNoEvents(string streamId, int events)
        {
            this.Context.HeaderMetadata = TestSetup.GetMetadata();

            await WhenIWriteNewStreamWithEvents(streamId, events);
        }

        [When(@"I append (\d+) events to stream ([^\s-]) in expected version (\d+)")]
        public async Task WhenIAppendEventsToStreamInExpectedVersion(int events, string streamId, ushort expectedVersion)
        {
            this.Context.StreamId = streamId;
            this.Context.NewEvents = TestSetup.GetEvents(events, expectedVersion + 1);
            this.Context.ExistingDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Container);

            var response = await this.Context.EventStore.WriteToStream(this.Context.StreamId, this.Context.NewEvents, expectedVersion, metadata: this.Context.HeaderMetadata);

            this.Context.Response = response;
        }

        [Then(@"the header version (\d+) with no metadata is persisted")]
        public async Task ThenTheHeaderVersionWithNoMetadataIsPersisted(ulong version)
        {
            var headerDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Header);

            Assert.AreEqual(1, headerDocuments.Count);

            var headerDocument = headerDocuments[0];

            Assert.AreEqual(DocumentType.Header, headerDocument.DocumentType);
            Assert.AreEqual(this.Context.StreamId, headerDocument.StreamId);
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
            var typeProvider = this.Context.EventStoreOptions.TypeProvider ?? new PlatformTypeProvider(this.Context.EventStoreOptions.IgnoreMissingTypes);

            var headerDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Header);

            Assert.AreEqual(1, headerDocuments.Count);

            var headerDocument = headerDocuments[0];

            Assert.AreEqual(DocumentType.Header, headerDocument.DocumentType);
            Assert.AreEqual(this.Context.StreamId, headerDocument.StreamId);
            Assert.AreEqual(version, headerDocument.Version);
            Assert.AreEqual(version + EveneumDocument.GetOrderingFraction(DocumentType.Header), headerDocument.SortOrder);
            Assert.AreEqual(typeProvider.GetIdentifierForType(typeof(SampleMetadata)), headerDocument.MetadataType);
            Assert.NotNull(headerDocument.Metadata);
            Assert.AreEqual(JToken.FromObject(this.Context.HeaderMetadata), headerDocument.Metadata);
            Assert.NotNull(headerDocument.ETag);
            Assert.False(headerDocument.Deleted);
        }

        [Then(@"the action fails as stream ([^\s-]) already exists")]
        public void ThenTheActionFailsAsStreamAlreadyExists(string streamId)
        {
            Assert.IsInstanceOf<StreamAlreadyExistsException>(this.ScenarioContext.TestError);

            var exception = this.ScenarioContext.TestError as StreamAlreadyExistsException;
            Assert.AreEqual(streamId, exception.StreamId);
        }

        [Then(@"the action fails as stream ([^\s-]) doesn't exist")]
        public void ThenTheActionFailsAsStreamDoesntExist(string streamId)
        {
            Assert.IsInstanceOf<StreamNotFoundException>(this.ScenarioContext.TestError);

            var exception = this.ScenarioContext.TestError as StreamNotFoundException;
            Assert.AreEqual(streamId, exception.StreamId);
        }

        [Then(@"the action fails as stream ([^\s-]) has been deleted")]
        public void ThenTheActionFailsAsStreamHasBeenDeleted(string streamId)
        {
            Assert.IsInstanceOf<StreamDeletedException>(this.ScenarioContext.TestError);

            var exception = this.ScenarioContext.TestError as StreamDeletedException;
            Assert.AreEqual(streamId, exception.StreamId);
        }

        [Then(@"the action fails as expected version (\d+) doesn't match the current version (\d+) of stream ([^\s-])")]
        public void ThenTheActionFailsAsExpectedVersionDoesnTMatchTheCurrentVersionOfStream(ulong expectedVersion, ulong currentVersion, string streamId)
        {
            Assert.IsInstanceOf<OptimisticConcurrencyException>(this.ScenarioContext.TestError);

            var exception = this.ScenarioContext.TestError as OptimisticConcurrencyException;
            Assert.AreEqual(streamId, exception.StreamId);
            Assert.AreEqual(expectedVersion, exception.ExpectedVersion);
            Assert.AreEqual(currentVersion, exception.ActualVersion);
        }

        [Then(@"no events are appended")]
        public async Task ThenNoEventsAreAppended()
        {
            var streamId = this.Context.StreamId;
            var currentDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId, DocumentType.Event);
            var existingDocumentIds = this.Context.ExistingDocuments.Select(x => x.Id);

            var newEventDocuments = currentDocuments.Where(x => !existingDocumentIds.Contains(x.Id));

            Assert.IsEmpty(newEventDocuments);
        }

        [Then(@"new events are appended")]
        public async Task ThenNewEventsAreAppended()
        {
            var typeProvider = this.Context.EventStoreOptions.TypeProvider ?? new PlatformTypeProvider(this.Context.EventStoreOptions.IgnoreMissingTypes);

            var streamId = this.Context.StreamId;
            var currentDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId, DocumentType.Event);
            var existingDocumentIds = this.Context.ExistingDocuments.Select(x => x.Id);

            var newEventDocuments = currentDocuments.Where(x => !existingDocumentIds.Contains(x.Id)).ToList();

            var newEvents = this.Context.NewEvents;

            Assert.AreEqual(newEventDocuments.Count, newEvents.Length);

            foreach (var newEvent in newEvents)
            {
                var eventDocument = newEventDocuments.Find(x => x.Id == EveneumDocument.GenerateEventId(streamId, newEvent.Version));

                Assert.IsNotNull(eventDocument);
                Assert.AreEqual(DocumentType.Event, eventDocument.DocumentType);
                Assert.AreEqual(streamId, eventDocument.StreamId);
                Assert.AreEqual(typeProvider.GetIdentifierForType(newEvent.Body.GetType()), eventDocument.BodyType);
                Assert.NotNull(eventDocument.Body);
                Assert.AreEqual(JToken.FromObject(newEvent.Body, JsonSerializer.Create(this.Context.JsonSerializerSettings)), eventDocument.Body);
                Assert.NotNull(eventDocument.ETag);
                Assert.False(eventDocument.Deleted);

                if (newEvent.Metadata == null)
                {
                    Assert.IsNull(eventDocument.MetadataType);
                    Assert.IsNull(eventDocument.Metadata);
                }
                else
                {
                    Assert.AreEqual(typeProvider.GetIdentifierForType(newEvent.Metadata.GetType()), eventDocument.MetadataType);
                    Assert.NotNull(eventDocument.Metadata);
                    Assert.AreEqual(JToken.FromObject(newEvent.Metadata, JsonSerializer.Create(this.Context.JsonSerializerSettings)), eventDocument.Metadata);
                }
            }
        }
    }
}
