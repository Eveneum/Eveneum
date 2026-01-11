using Eveneum.Advanced;
using Eveneum.Documents;
using Eveneum.Serialization;
using Eveneum.Tests.Infrastructure;
using Microsoft.Azure.Cosmos;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Eveneum.Tests
{
    [Binding]
    public class AdvancedSteps(NewtonsoftCosmosDbContext newtonsoftContext, SystemTextJsonCosmosDbContext stjContext)
    {
        private readonly IReadOnlyCollection<CosmosDbContext> Contexts = [newtonsoftContext, stjContext];

        [When(@"I load all events")]
        public async Task WhenILoadAllEvents()
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var events = new List<EventData>();
                var response = await (x.EventStore as IAdvancedEventStore).LoadAllEvents(e => { events.AddRange(e); return Task.CompletedTask; });
                x.LoadAllEvents = events;
                x.Response = response;
            }));
        }

        [When(@"I load events using query text (.*)")]
        public async Task WhenIQueryEventsUsingQueryText(string query)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var events = new List<EventData>();
                var response = await (x.EventStore as IAdvancedEventStore).LoadEvents(query, e => { events.AddRange(e); return Task.CompletedTask; });
                x.LoadAllEvents = events;
                x.Response = response;
            }));
        }

        [When(@"I load events using query definition (.*)")]
        public async Task WhenIQueryEventsUsingQueryDefinition(string query)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var events = new List<EventData>();
                var response = await (x.EventStore as IAdvancedEventStore).LoadEvents(new QueryDefinition(query), e => { events.AddRange(e); return Task.CompletedTask; });
                x.LoadAllEvents = events;
                x.Response = response;
            }));
        }

        [When(@"I load stream headers using query text(.*)")]
        public async Task WhenIQueryStreamHeadersUsingQueryText(string query)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var headers = new List<StreamHeader>();
                var response = await (x.EventStore as IAdvancedEventStore).LoadStreamHeaders(query, e => { headers.AddRange(e); return Task.CompletedTask; });
                x.LoadAllStreamHeaders = headers;
                x.Response = response;
            }));
        }

        [When(@"I load stream headers using query definition (.*)")]
        public async Task WhenIQueryStreamHeadersUsingQueryDefinition(string query)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                var headers = new List<StreamHeader>();
                var response = await (x.EventStore as IAdvancedEventStore).LoadStreamHeaders(new QueryDefinition(query), e => { headers.AddRange(e); return Task.CompletedTask; });
                x.LoadAllStreamHeaders = headers;
                x.Response = response;
            }));
        }

        [When(@"I replace event in version (\d+) in stream (.*)")]
        public async Task WhenIReplaceEventInVersionInStream(ulong version, string streamId)
        {
            var replacedEvent = TestSetup.GetEvents(1, (int)version, streamId)[0];

            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                x.ReplacedEvent = replacedEvent;
                var response = await (x.EventStore as IAdvancedEventStore).ReplaceEvent(replacedEvent);
                x.Response = response;
            }));
        }
        
        [When(@"I delete event in version (\d+) in stream (.*)")]
        public async Task WhenIDeleteEventInVersionInStreamB(ulong version, string streamId)
        {
            await Task.WhenAll(this.Contexts.Select(async x =>
            {
                x.Response = await (x.EventStore as IAdvancedEventStore).DeleteEvent(streamId, version);
            }));
        }

        [Then(@"all (\d+) events are loaded")]
        public void ThenAllEventsAreLoaded(ulong events)
        {
            foreach (var context in this.Contexts)
            {
                Assert.That((ulong)context.LoadAllEvents.Count, Is.EqualTo(events));
            }
        }

        [Then(@"the stream header for stream (.*) in version (\d+) is returned")]
        public void ThenTheStreamHeaderForStreamInVersionIsReturned(string streamId, ulong version)
        {
            foreach (var context in this.Contexts)
            {
                Assert.That(context.LoadAllStreamHeaders.Any(x => x.StreamId == streamId && x.Version == version));
            }
        }

        [Then(@"the stream header for stream (.*) in version (\d+) is not returned")]
        public void ThenTheStreamHeaderForStreamInVersionIsNotReturned(string streamId, ulong version)
        {
            foreach (var context in this.Contexts)
            {
                Assert.That(context.LoadAllStreamHeaders.Any(x => x.StreamId == streamId && x.Version == version), Is.False);
            }
        }

        [Then(@"the event in version (\d+) in stream (.*) is replaced")]
        public async Task ThenTheEventInVersionInStreamIsReplaced(ulong version, string streamId)
        {
            await Task.WhenAll(this.Contexts.Select(async context =>
            {
                var typeProvider = context.EventStoreOptions.TypeProvider ?? new PlatformTypeProvider(context.EventStoreOptions.IgnoreMissingTypes);
                var documents = await CosmosSetup.QueryAllDocumentsInStream(context.Client, context.Database, context.Container, streamId, DocumentType.Event);
                var eventDocument = documents.SingleOrDefault(x => x.Id == EveneumDocumentSerializer.GenerateEventId(streamId, version));

                Assert.That(eventDocument.DocumentType, Is.EqualTo(DocumentType.Event));
                Assert.That(eventDocument.StreamId, Is.EqualTo(streamId));
                Assert.That(eventDocument.BodyType, Is.EqualTo(typeProvider.GetIdentifierForType(context.ReplacedEvent.Body.GetType())));
                Assert.That(eventDocument.Body, Is.Not.Null);
                Assert.That(context.AreEqual(eventDocument.Body, context.ReplacedEvent.Body), Is.True);
                Assert.That(eventDocument.ETag, Is.Not.Null);
                Assert.That(eventDocument.Deleted, Is.False);

                if (context.ReplacedEvent.Metadata == null)
                {
                    Assert.That(eventDocument.MetadataType, Is.Null);
                    Assert.That(eventDocument.Metadata, Is.Null);
                }
                else
                {
                    Assert.That(eventDocument.MetadataType, Is.EqualTo(typeProvider.GetIdentifierForType(context.ReplacedEvent.Metadata.GetType())));
                    Assert.That(eventDocument.Metadata, Is.Not.Null);
                    Assert.That(context.AreEqual(eventDocument.Metadata, context.ReplacedEvent.Metadata), Is.True);
                }
            }));
        }

        [Then(@"the event in version (\d+) in stream (.*) is soft-deleted")]
        public async Task ThenTheEventInVersionInStreamIsSoftDeleted(ulong version, string streamId)
        {
            await Task.WhenAll(this.Contexts.Select(async context =>
            {
                var documents = await CosmosSetup.QueryAllDocumentsInStream(context.Client, context.Database, context.Container, context.StreamId, DocumentType.Event);
                
                Assert.That(documents.Single(x => x.Version == version).Deleted, Is.True);
            }));
        }

        [Then(@"the event in version (\d+) in stream (.*) is hard-deleted")]
        public async Task ThenTheEventInVersionInStreamIsHardDeleted(ulong version, string streamId)
        {
            await Task.WhenAll(this.Contexts.Select(async context =>
            {
                var documents = await CosmosSetup.QueryAllDocumentsInStream(context.Client, context.Database, context.Container, context.StreamId, DocumentType.Event);

                Assert.That(documents.SingleOrDefault(x => x.Version == version), Is.Null);
            }));
        }
    }
}
