using System.Threading.Tasks;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastrature;
using Eveneum.Advanced;
using System.Collections.Generic;
using NUnit.Framework;
using Eveneum.Documents;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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

        [When(@"I load events using query (.*)")]
        public async Task WhenIQueryEventsUsing(string query)
        {
            var events = new List<EventData>();

            var response = await (this.Context.EventStore as IAdvancedEventStore).LoadEvents(query, e => { events.AddRange(e); return Task.CompletedTask; });

            this.Context.LoadAllEvents = events;
            this.Context.Response = response;
        }

        [When(@"I load stream headers using query (.*)")]
        public async Task WhenIQueryStreamHeadersUsing(string query)
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
            Assert.AreEqual(events, this.Context.LoadAllEvents.Count);
        }

        [Then(@"the stream header for stream (.*) in version (\d+) is returned")]
        public void ThenTheStreamHeaderForStreamInVersionIsReturned(string streamId, ulong version)
        {
            Assert.IsNotNull(this.Context.LoadAllStreamHeaders.Find(x => x.StreamId == streamId && x.Version == version));
        }

        [Then(@"the event in version (\d+) in stream (.*) is replaced")]
        public async Task ThenTheEventInVersionInStreamIsReplaced(ulong version, string streamId)
        {
            var currentDocuments = await CosmosSetup.QueryAllDocumentsInStream(this.Context.Client, this.Context.Database, this.Context.Container, streamId, DocumentType.Event);
            var eventDocument = currentDocuments.SingleOrDefault(x => x.Id == EveneumDocument.GenerateEventId(streamId, version));

            Assert.AreEqual(DocumentType.Event, eventDocument.DocumentType);
            Assert.AreEqual(streamId, eventDocument.StreamId);
            Assert.AreEqual(this.Context.EventStoreOptions.TypeProvider.GetIdentifierForType(this.Context.ReplacedEvent.Body.GetType()), eventDocument.BodyType);
            Assert.NotNull(eventDocument.Body);
            Assert.AreEqual(JToken.FromObject(this.Context.ReplacedEvent.Body, JsonSerializer.Create(this.Context.JsonSerializerSettings)), eventDocument.Body);
            Assert.NotNull(eventDocument.ETag);
            Assert.False(eventDocument.Deleted);

            if (this.Context.ReplacedEvent.Metadata == null)
            {
                Assert.IsNull(eventDocument.MetadataType);
                Assert.IsNull(eventDocument.Metadata);
            }
            else
            {
                Assert.AreEqual(this.Context.EventStoreOptions.TypeProvider.GetIdentifierForType(this.Context.ReplacedEvent.Metadata.GetType()), eventDocument.MetadataType);
                Assert.NotNull(eventDocument.Metadata);
                Assert.AreEqual(JToken.FromObject(this.Context.ReplacedEvent.Metadata, JsonSerializer.Create(this.Context.JsonSerializerSettings)), eventDocument.Metadata);
            }
        }
    }
}
