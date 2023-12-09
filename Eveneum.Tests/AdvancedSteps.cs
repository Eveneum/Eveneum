using Eveneum.Advanced;
using Eveneum.Documents;
using Eveneum.Serialization;
using Eveneum.Tests.Infrastructure;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Eveneum.Tests
{
    [Binding]
    public class AdvancedSteps
    {
        private readonly CosmosDbContext Context;

        AdvancedSteps(CosmosDbContext context)
        {
            this.Context = context;
        }

        [When(@"I load all events")]
        public async Task WhenILoadAllEvents()
        {
            var events = new List<EventData>();

            var response = await (this.Context.EventStore as IAdvancedEventStore).LoadAllEvents(e => { events.AddRange(e); return Task.CompletedTask; });

            this.Context.LoadAllEvents = events;
            this.Context.Response = response;
        }

        [When(@"I load events using query text (.*)")]
        public async Task WhenIQueryEventsUsingQueryText(string query)
        {
            var events = new List<EventData>();

            var response = await (this.Context.EventStore as IAdvancedEventStore).LoadEvents(query, e => { events.AddRange(e); return Task.CompletedTask; });

            this.Context.LoadAllEvents = events;
            this.Context.Response = response;
        }

        [When(@"I load events using query definition (.*)")]
        public async Task WhenIQueryEventsUsingQueryDefinition(string query)
        {
            var events = new List<EventData>();

            var response = await (this.Context.EventStore as IAdvancedEventStore).LoadEvents(new QueryDefinition(query), e => { events.AddRange(e); return Task.CompletedTask; });

            this.Context.LoadAllEvents = events;
            this.Context.Response = response;
        }

        [When(@"I load stream headers using query text(.*)")]
        public async Task WhenIQueryStreamHeadersUsingQueryText(string query)
        {
            var headers = new List<StreamHeader>();

            var response = await (this.Context.EventStore as IAdvancedEventStore).LoadStreamHeaders(query, e => { headers.AddRange(e); return Task.CompletedTask; });

            this.Context.LoadAllStreamHeaders = headers;
            this.Context.Response = response;
        }

        [When(@"I load stream headers using query definition (.*)")]
        public async Task WhenIQueryStreamHeadersUsingQueryDefinition(string query)
        {
            var headers = new List<StreamHeader>();

            var response = await (this.Context.EventStore as IAdvancedEventStore).LoadStreamHeaders(query, e => { headers.AddRange(e); return Task.CompletedTask; });

            this.Context.LoadAllStreamHeaders = headers;
            this.Context.Response = response;
        }

        [When(@"I replace event in version (\d+) in stream (.*)")]
        public async Task WhenIReplaceEventInVersionInStream(ulong version, string streamId)
        {
            this.Context.ReplacedEvent = TestSetup.GetEvents(1, (int)version, streamId)[0];

            var response = await (this.Context.EventStore as IAdvancedEventStore).ReplaceEvent(this.Context.ReplacedEvent);

            this.Context.Response = response;
        }

        [Then(@"all (\d+) events are loaded")]
        public void ThenAllEventsAreLoaded(ulong events)
        {
            Assert.That(events, Is.EqualTo(this.Context.LoadAllEvents.Count));
        }

        [Then(@"the stream header for stream (.*) in version (\d+) is returned")]
        public void ThenTheStreamHeaderForStreamInVersionIsReturned(string streamId, ulong version)
        {
            Assert.That(this.Context.LoadAllStreamHeaders.Any(x => x.StreamId == streamId && x.Version == version));
        }

        [Then(@"the stream header for stream (.*) in version (\d+) is not returned")]
        public void ThenTheStreamHeaderForStreamInVersionIsNotReturned(string streamId, ulong version)
        {
            Assert.That(this.Context.LoadAllStreamHeaders.Any(x => x.StreamId == streamId && x.Version == version), Is.False);
        }

        [Then(@"the event in version (\d+) in stream (.*) is replaced")]
        public async Task ThenTheEventInVersionInStreamIsReplaced(ulong version, string streamId)
        {
            var typeProvider = this.Context.EventStoreOptions.TypeProvider ?? new PlatformTypeProvider(this.Context.EventStoreOptions.IgnoreMissingTypes);

            var currentDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId, DocumentType.Event);
            var eventDocument = currentDocuments.SingleOrDefault(x => x.Id == EveneumDocumentSerializer.GenerateEventId(streamId, version));

            Assert.That(eventDocument.DocumentType, Is.EqualTo(DocumentType.Event));
            Assert.That(eventDocument.StreamId, Is.EqualTo(streamId));
            Assert.That(eventDocument.BodyType, Is.EqualTo(typeProvider.GetIdentifierForType(this.Context.ReplacedEvent.Body.GetType())));
            Assert.That(eventDocument.Body, Is.Not.Null);
            Assert.That(eventDocument.Body, Is.EqualTo(JToken.FromObject(this.Context.ReplacedEvent.Body, JsonSerializer.Create(this.Context.JsonSerializerSettings))));
            Assert.That(eventDocument.ETag, Is.Not.Null);
            Assert.That(eventDocument.Deleted, Is.False);

            if (this.Context.ReplacedEvent.Metadata == null)
            {
                Assert.That(eventDocument.MetadataType, Is.Null);
                Assert.That(eventDocument.Metadata, Is.Null);
            }
            else
            {
                Assert.That(eventDocument.MetadataType, Is.EqualTo(typeProvider.GetIdentifierForType(this.Context.ReplacedEvent.Metadata.GetType())));
                Assert.That(eventDocument.Metadata, Is.Not.Null);
                Assert.That(eventDocument.Metadata, Is.EqualTo(JToken.FromObject(this.Context.ReplacedEvent.Metadata, JsonSerializer.Create(this.Context.JsonSerializerSettings))));
            }
        }
    }
}
