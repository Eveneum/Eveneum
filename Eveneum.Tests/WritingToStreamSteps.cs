using Eveneum.Documents;
using Eveneum.Serialization;
using Eveneum.Tests.Infrastructure;
using NUnit.Framework;
using Reqnroll;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eveneum.Tests
{
    [Binding]
    public class WritingToStreamSteps(NewtonsoftCosmosDbContext newtonsoftContext, SystemTextJsonCosmosDbContext stjContext, ScenarioContext scenarioContext)
    {
        private readonly IReadOnlyCollection<CosmosDbContext> Contexts = [newtonsoftContext, stjContext];

        [When("I write a new stream {word} with {int} events")]
        public async Task WhenIWriteNewStreamWithEvents(string streamId, int events)
        {
            var eventsData = TestSetup.GetEvents(events);

            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                x.StreamId = streamId;
                x.NewEvents = eventsData;

                x.ExistingDocuments = await CosmosSetup.QueryAllDocuments(x.Client, x.Database, x.Container);
                x.Response = await x.EventStore.WriteToStream(streamId, eventsData, metadata: x.HeaderMetadata);
            }));
        }

        [When("I write a new stream {word} with metadata and {int} events")]
        public async Task WhenIWriteNewStreamWithMetadataAndNoEvents(string streamId, int events)
        {
            var metadata = TestSetup.GetMetadata();

            foreach (var context in this.Contexts)
                context.HeaderMetadata = metadata;

            await WhenIWriteNewStreamWithEvents(streamId, events);
        }

        [When("I append {int} events to stream {word} in expected version {int}")]
        public async Task WhenIAppendEventsToStreamInExpectedVersion(int events, string streamId, ushort expectedVersion)
        {
            var eventsData = TestSetup.GetEvents(events, expectedVersion + 1);

            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                x.StreamId = streamId;
                x.NewEvents = eventsData;

                x.ExistingDocuments = await CosmosSetup.QueryAllDocuments(x.Client, x.Database, x.Container);
                x.Response = await x.EventStore.WriteToStream(streamId, eventsData, expectedVersion, metadata: x.HeaderMetadata);
            }));
        }

        [When("I append events with version {word} to stream {word} in expected version {int}")]
        public async Task WhenIAppendEventsWithVersionToStreamInExpectedVersion(string versions, string streamId, ushort expectedVersion)
        {
            var eventVersions = versions
                .Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToUInt16(x));

            var eventsData = eventVersions.SelectMany(x => TestSetup.GetEvents(1, x)).ToArray();

            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                x.StreamId = streamId;
                x.NewEvents = eventsData;

                x.ExistingDocuments = await CosmosSetup.QueryAllDocuments(x.Client, x.Database, x.Container);
                x.Response = await x.EventStore.WriteToStream(streamId, eventsData, expectedVersion, metadata: x.HeaderMetadata);
            }));
        }

        [When("I append {int} events and events with version {word} to stream {word} in expected version {int}")]
        public async Task WhenIAppendEventsAndEventsWithVersionToStreamInExpectedVersion(int events, string versions, string streamId, ushort expectedVersion)
        {
            var eventVersions = versions
                .Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries)
                .Select(x => Convert.ToUInt16(x));

            var allEvents = new List<EventData>(TestSetup.GetEvents(events, expectedVersion + 1));
            allEvents.AddRange(eventVersions.SelectMany(x => TestSetup.GetEvents(1, x)));

            var eventsData = allEvents.ToArray();

            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                x.StreamId = streamId;
                x.NewEvents = eventsData;

                x.ExistingDocuments = await CosmosSetup.QueryAllDocuments(x.Client, x.Database, x.Container);
                x.Response = await x.EventStore.WriteToStream(streamId, eventsData, expectedVersion, metadata: x.HeaderMetadata);
            }));
        }

        [Then("the header version {int} with no metadata is persisted")]
        public async Task ThenTheHeaderVersionWithNoMetadataIsPersisted(ulong version)
        {
            await Task.WhenAll(this.Contexts.Select(async context =>
            {
                var headerDocuments = await CosmosSetup.QueryAllDocumentsInStream(context.Client, context.Database, context.Container, context.StreamId, DocumentType.Header);

                Assert.That(headerDocuments, Has.Count.EqualTo(1));

                var headerDocument = headerDocuments[0];

                Assert.That(headerDocument.DocumentType, Is.EqualTo(DocumentType.Header));
                Assert.That(headerDocument.StreamId, Is.EqualTo(context.StreamId));
                Assert.That(headerDocument.Version, Is.EqualTo(version));
                Assert.That(headerDocument.SortOrder, Is.EqualTo(version + EveneumDocument.GetOrderingFraction(DocumentType.Header)));
                Assert.That(headerDocument.MetadataType, Is.Null);
                Assert.That(headerDocument.Metadata, Is.Null);
                Assert.That(headerDocument.ETag, Is.Not.Null);
                Assert.That(headerDocument.Deleted, Is.False);
            }));
        }

        [Then("the header version {int} with metadata is persisted")]
        public async Task ThenTheHeaderVersionWithMetadataIsPersisted(ulong version)
        {
            await Task.WhenAll(this.Contexts.Select(async context =>
            {
                var typeProvider = context.EventStoreOptions.TypeProvider ?? new PlatformTypeProvider(context.EventStoreOptions.IgnoreMissingTypes);

                var headerDocuments = await CosmosSetup.QueryAllDocumentsInStream(context.Client, context.Database, context.Container, context.StreamId, DocumentType.Header);

                Assert.That(headerDocuments.Count, Is.EqualTo(1));

                var headerDocument = headerDocuments[0];

                Assert.That(headerDocument.DocumentType, Is.EqualTo(DocumentType.Header));
                Assert.That(headerDocument.StreamId, Is.EqualTo(context.StreamId));
                Assert.That(headerDocument.Version, Is.EqualTo(version));
                Assert.That(headerDocument.SortOrder, Is.EqualTo(version + EveneumDocument.GetOrderingFraction(DocumentType.Header)));
                Assert.That(headerDocument.MetadataType, Is.EqualTo(typeProvider.GetIdentifierForType(typeof(SampleMetadata))));
                Assert.That(headerDocument.Metadata, Is.Not.Null);
                Assert.That(context.AreEqual(headerDocument.Metadata, context.HeaderMetadata), Is.True);
                Assert.That(headerDocument.ETag, Is.Not.Null);
                Assert.That(headerDocument.Deleted, Is.False);
            }));
        }

        [Then("the action fails as stream {word} already exists")]
        public void ThenTheActionFailsAsStreamAlreadyExists(string streamId)
        {
            Assert.That(scenarioContext.TestError, Is.InstanceOf<StreamAlreadyExistsException>());

            var exception = scenarioContext.TestError as StreamAlreadyExistsException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
        }

        [Then("the action fails as stream {word} doesn't exist")]
        public void ThenTheActionFailsAsStreamDoesntExist(string streamId)
        {
            Assert.That(scenarioContext.TestError, Is.InstanceOf<StreamNotFoundException>());

            var exception = scenarioContext.TestError as StreamNotFoundException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
        }

        [Then("the action fails as stream {word} has been deleted")]
        public void ThenTheActionFailsAsStreamHasBeenDeleted(string streamId)
        {
            Assert.That(scenarioContext.TestError, Is.InstanceOf<StreamDeletedException>());

            var exception = scenarioContext.TestError as StreamDeletedException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
        }

        [Then("the action fails as expected version {int} doesn't match the current version {int} of stream {word}")]
        public void ThenTheActionFailsAsExpectedVersionDoesntMatchTheCurrentVersionOfStream(ulong expectedVersion, ulong currentVersion, string streamId)
        {
            Assert.That(scenarioContext.TestError, Is.InstanceOf<OptimisticConcurrencyException>());

            var exception = scenarioContext.TestError as OptimisticConcurrencyException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
            Assert.That(exception.ExpectedVersion, Is.EqualTo(expectedVersion));
            Assert.That(exception.ActualVersion, Is.EqualTo(currentVersion));
        }

        [Then(@"no events are appended")]
        public async Task ThenNoEventsAreAppended()
        {
            await Task.WhenAll(this.Contexts.Select(async context =>
            {
                var streamId = context.StreamId;
                var currentDocuments = await CosmosSetup.QueryAllDocumentsInStream(context.Client, context.Database, context.Container, streamId, DocumentType.Event);
                var existingDocumentIds = context.ExistingDocuments.Select(x => x.Id);

                var newEventDocuments = currentDocuments.Where(x => !existingDocumentIds.Contains(x.Id));

                Assert.That(newEventDocuments, Is.Empty);
            }));
        }

        [Then(@"new events are appended")]
        public async Task ThenNewEventsAreAppended()
        {
            await Task.WhenAll(this.Contexts.Select(async context =>
            {
                var streamId = context.StreamId;
                var currentDocuments = await CosmosSetup.QueryAllDocumentsInStream(context.Client, context.Database, context.Container, streamId, DocumentType.Event);
                var existingDocumentIds = context.ExistingDocuments.Select(x => x.Id);

                var newEventDocuments = currentDocuments.Where(x => !existingDocumentIds.Contains(x.Id)).ToList();
                var newEvents = context.NewEvents;

                VerifyEventDocuments(context, newEventDocuments, newEvents);
            }));
        }

        [Then("first {int} events are appended")]
        public async Task ThenFirstEventsAreAppended(int events)
        {
            await Task.WhenAll(this.Contexts.Select(async context =>
            {
                var typeProvider = context.EventStoreOptions.TypeProvider ?? new PlatformTypeProvider(context.EventStoreOptions.IgnoreMissingTypes);

                var currentDocuments = await CosmosSetup.QueryAllDocumentsInStream(context.Client, context.Database, context.Container, context.StreamId, DocumentType.Event);
                var existingDocumentIds = context.ExistingDocuments.Select(x => x.Id);

                var newEventDocuments = currentDocuments.Where(x => !existingDocumentIds.Contains(x.Id)).ToList();
                var newEvents = context.NewEvents.Take(events).ToArray();

                VerifyEventDocuments(context, newEventDocuments, newEvents);
            }));
        }

        [Then("the action fails as event with version {int} already exists in stream {word}")]
        public void ThenTheActionFailsAsEventWithVersionAlreadyExistsInStream(ulong version, string streamId)
        {
            Assert.That(scenarioContext.TestError, Is.InstanceOf<EventAlreadyExistsException>());

            var exception = scenarioContext.TestError as EventAlreadyExistsException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
            Assert.That(exception.Version, Is.EqualTo(version));
        }

        private static void VerifyEventDocuments(CosmosDbContext context, List<IEveneumDocument> newEventDocuments, EventData[] newEvents)
        {
            Assert.That(newEvents.Length, Is.EqualTo(newEventDocuments.Count));

            var streamId = context.StreamId;
            var typeProvider = context.EventStoreOptions.TypeProvider ?? new PlatformTypeProvider(context.EventStoreOptions.IgnoreMissingTypes);

            foreach (var newEvent in newEvents)
            {
                var eventDocument = newEventDocuments.Find(x => x.Id == EveneumDocumentSerializer.GenerateEventId(streamId, newEvent.Version));

                Assert.That(eventDocument, Is.Not.Null);
                Assert.That(eventDocument.DocumentType, Is.EqualTo(DocumentType.Event));
                Assert.That(eventDocument.StreamId, Is.EqualTo(streamId));
                Assert.That(eventDocument.BodyType, Is.EqualTo(typeProvider.GetIdentifierForType(newEvent.Body.GetType())));
                Assert.That(eventDocument.Body, Is.Not.Null);
                Assert.That(context.AreEqual(eventDocument.Body, newEvent.Body), Is.True);
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
                    Assert.That(context.AreEqual(eventDocument.Metadata, newEvent.Metadata), Is.True);
                }
            }
        }
    }
}

