using System.Threading.Tasks;
using NUnit.Framework;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastructure;
using Eveneum.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Eveneum.Serialization;
using System;
using System.IO;
using System.Collections.Generic;

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

        [When(@"I append events with version ([\d, ]+) to stream ([^\s-]) in expected version (\d+)")]
        public async Task WhenIAppendEventsWithVersionToStreamInExpectedVersion(string versions, string streamId, ushort expectedVersion)
        {
            var eventVersions = versions
                .Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToUInt16(x));

            var events = eventVersions.SelectMany(x => TestSetup.GetEvents(1, x)).ToArray();

            this.Context.StreamId = streamId;
            this.Context.NewEvents = events;
            this.Context.ExistingDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Container);

            var response = await this.Context.EventStore.WriteToStream(this.Context.StreamId, this.Context.NewEvents, expectedVersion, metadata: this.Context.HeaderMetadata);

            this.Context.Response = response;
        }

        [When(@"I append (\d+) events and events with version ([\d, ]+) to stream ([^\s-]) in expected version (\d+)")]
        public async Task WhenIAppendEventsAndEventsWithVersionToStreamInExpectedVersion(int events, string versions, string streamId, ushort expectedVersion)
        {
            var eventVersions = versions
                .Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToUInt16(x));

            var allEvents = new List<EventData>(TestSetup.GetEvents(events, expectedVersion + 1));
            allEvents.AddRange(eventVersions.SelectMany(x => TestSetup.GetEvents(1, x)));

            this.Context.StreamId = streamId;
            this.Context.NewEvents = allEvents.ToArray();
            this.Context.ExistingDocuments = await CosmosSetup.QueryAllDocuments(this.Context.Client, this.Context.Database, this.Context.Container);

            var response = await this.Context.EventStore.WriteToStream(this.Context.StreamId, this.Context.NewEvents, expectedVersion, metadata: this.Context.HeaderMetadata);

            this.Context.Response = response;
        }

        [Then(@"the header version (\d+) with no metadata is persisted")]
        public async Task ThenTheHeaderVersionWithNoMetadataIsPersisted(ulong version)
        {
            var headerDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Header);

            Assert.That(headerDocuments.Count, Is.EqualTo(1));

            var headerDocument = headerDocuments[0];

            Assert.That(headerDocument.DocumentType, Is.EqualTo(DocumentType.Header));
            Assert.That(headerDocument.StreamId, Is.EqualTo(this.Context.StreamId));
            Assert.That(headerDocument.Version, Is.EqualTo(version));
            Assert.That(headerDocument.SortOrder, Is.EqualTo(version + EveneumDocument.GetOrderingFraction(DocumentType.Header)));
            Assert.That(headerDocument.MetadataType, Is.Null);
            Assert.That(headerDocument.Metadata.HasValues, Is.False);
            Assert.That(headerDocument.ETag, Is.Not.Null);
            Assert.That(headerDocument.Deleted, Is.False);
        }

        [Then(@"the header version (\d+) with metadata is persisted")]
        public async Task ThenTheHeaderVersionWithMetadataIsPersisted(ulong version)
        {
            var typeProvider = this.Context.EventStoreOptions.TypeProvider ?? new PlatformTypeProvider(this.Context.EventStoreOptions.IgnoreMissingTypes);

            var headerDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Header);

            Assert.That(headerDocuments.Count, Is.EqualTo(1));

            var headerDocument = headerDocuments[0];

            Assert.That(headerDocument.DocumentType, Is.EqualTo(DocumentType.Header));
            Assert.That(headerDocument.StreamId, Is.EqualTo(this.Context.StreamId));
            Assert.That(headerDocument.Version, Is.EqualTo(version));
            Assert.That(headerDocument.SortOrder, Is.EqualTo(version + EveneumDocument.GetOrderingFraction(DocumentType.Header)));
            Assert.That(headerDocument.MetadataType, Is.EqualTo(typeProvider.GetIdentifierForType(typeof(SampleMetadata))));
            Assert.That(headerDocument.Metadata, Is.Not.Null);
            Assert.That(headerDocument.Metadata, Is.EqualTo(JToken.FromObject(this.Context.HeaderMetadata)));
            Assert.That(headerDocument.ETag, Is.Not.Null);
            Assert.That(headerDocument.Deleted, Is.False);
        }

        [Then(@"the action fails as stream ([^\s-]) already exists")]
        public void ThenTheActionFailsAsStreamAlreadyExists(string streamId)
        {
            Assert.That(this.ScenarioContext.TestError, Is.InstanceOf<StreamAlreadyExistsException>());

            var exception = this.ScenarioContext.TestError as StreamAlreadyExistsException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
        }

        [Then(@"the action fails as stream ([^\s-]) doesn't exist")]
        public void ThenTheActionFailsAsStreamDoesntExist(string streamId)
        {
            Assert.That(this.ScenarioContext.TestError, Is.InstanceOf<StreamNotFoundException>());

            var exception = this.ScenarioContext.TestError as StreamNotFoundException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
        }

        [Then(@"the action fails as stream ([^\s-]) has been deleted")]
        public void ThenTheActionFailsAsStreamHasBeenDeleted(string streamId)
        {
            Assert.That(this.ScenarioContext.TestError, Is.InstanceOf<StreamDeletedException>());

            var exception = this.ScenarioContext.TestError as StreamDeletedException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
        }

        [Then(@"the action fails as expected version (\d+) doesn't match the current version (\d+) of stream ([^\s-])")]
        public void ThenTheActionFailsAsExpectedVersionDoesntMatchTheCurrentVersionOfStream(ulong expectedVersion, ulong currentVersion, string streamId)
        {
            Assert.That(this.ScenarioContext.TestError, Is.InstanceOf<OptimisticConcurrencyException>());

            var exception = this.ScenarioContext.TestError as OptimisticConcurrencyException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
            Assert.That(exception.ExpectedVersion, Is.EqualTo(expectedVersion));
            Assert.That(exception.ActualVersion, Is.EqualTo(currentVersion));
        }

        [Then(@"no events are appended")]
        public async Task ThenNoEventsAreAppended()
        {
            var streamId = this.Context.StreamId;
            var currentDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId, DocumentType.Event);
            var existingDocumentIds = this.Context.ExistingDocuments.Select(x => x.Id);

            var newEventDocuments = currentDocuments.Where(x => !existingDocumentIds.Contains(x.Id));

            Assert.That(newEventDocuments, Is.Empty);
        }

        [Then(@"new events are appended")]
        public async Task ThenNewEventsAreAppended()
        {
            var streamId = this.Context.StreamId;
            var currentDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId, DocumentType.Event);
            var existingDocumentIds = this.Context.ExistingDocuments.Select(x => x.Id);

            var newEventDocuments = currentDocuments.Where(x => !existingDocumentIds.Contains(x.Id)).ToList();
            var newEvents = this.Context.NewEvents;

            VerifyEventDocuments(newEventDocuments, newEvents);
        }

        [Then(@"first (\d+) events are appended")]
        public async Task ThenFirstEventsAreAppended(int events)
        {
            var typeProvider = this.Context.EventStoreOptions.TypeProvider ?? new PlatformTypeProvider(this.Context.EventStoreOptions.IgnoreMissingTypes);

            var currentDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, this.Context.StreamId, DocumentType.Event);
            var existingDocumentIds = this.Context.ExistingDocuments.Select(x => x.Id);

            var newEventDocuments = currentDocuments.Where(x => !existingDocumentIds.Contains(x.Id)).ToList();
            var newEvents = this.Context.NewEvents.Take(events).ToArray();

            VerifyEventDocuments(newEventDocuments, newEvents);
        }

        [Then(@"the action fails as event with version (\d+) already exists in stream ([^\s-])")]
        public void ThenTheActionFailsAsEventWithVersionAlreadyExistsInStream(ulong version, string streamId)
        {
            Assert.That(this.ScenarioContext.TestError, Is.InstanceOf<EventAlreadyExistsException>());

            var exception = this.ScenarioContext.TestError as EventAlreadyExistsException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
            Assert.That(exception.Version, Is.EqualTo(version));
        }

        private void VerifyEventDocuments(List<EveneumDocument> newEventDocuments, EventData[] newEvents)
        {
            Assert.That(newEvents.Length, Is.EqualTo(newEventDocuments.Count));

            var streamId = this.Context.StreamId;
            var typeProvider = this.Context.EventStoreOptions.TypeProvider ?? new PlatformTypeProvider(this.Context.EventStoreOptions.IgnoreMissingTypes);

            foreach (var newEvent in newEvents)
            {
                var eventDocument = newEventDocuments.Find(x => x.Id == EveneumDocumentSerializer.GenerateEventId(streamId, newEvent.Version));

                Assert.That(eventDocument, Is.Not.Null);
                Assert.That(eventDocument.DocumentType, Is.EqualTo(DocumentType.Event));
                Assert.That(eventDocument.StreamId, Is.EqualTo(streamId));
                Assert.That(eventDocument.BodyType, Is.EqualTo(typeProvider.GetIdentifierForType(newEvent.Body.GetType())));
                Assert.That(eventDocument.Body, Is.Not.Null);
                Assert.That(eventDocument.Body, Is.EqualTo(JToken.FromObject(newEvent.Body, JsonSerializer.Create(this.Context.JsonSerializerSettings))));
                Assert.That(eventDocument.ETag, Is.Not.Null);
                Assert.That(eventDocument.Deleted, Is.False);

                if (newEvent.Metadata == null)
                {
                    Assert.That(eventDocument.MetadataType, Is.Null);
                    Assert.That(eventDocument.Metadata, Is.Null);
                }
                else
                {
                    Assert.That(eventDocument.MetadataType, Is.EqualTo(typeProvider.GetIdentifierForType(newEvent.Metadata.GetType())));
                    Assert.That(eventDocument.Metadata, Is.Not.Null);
                    Assert.That(eventDocument.Metadata, Is.EqualTo(JToken.FromObject(newEvent.Metadata, JsonSerializer.Create(this.Context.JsonSerializerSettings))));
                }
            }
        }
    }
}
