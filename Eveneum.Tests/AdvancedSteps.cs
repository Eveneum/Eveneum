using System.Threading.Tasks;
using TechTalk.SpecFlow;
using Eveneum.Tests.Infrastrature;
using Eveneum.Advanced;
using System.Collections.Generic;
using NUnit.Framework;

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
    }
}
