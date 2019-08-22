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

            await (this.Context.EventStore as IAdvancedEventStore).LoadAllEvents(e => { events.AddRange(e); return Task.CompletedTask; });

            this.Context.LoadAllEvents = events;
        }

        [When(@"I load events using query (.*)")]
        public async Task WhenIQueryEventsUsing(string query)
        {
            var events = new List<EventData>();

            await(this.Context.EventStore as IAdvancedEventStore).LoadEvents(query, e => { events.AddRange(e); return Task.CompletedTask; });

            this.Context.LoadAllEvents = events;
        }

        [Then(@"all (\d+) events are loaded")]
        public void ThenAllEventsAreLoaded(ulong events)
        {
            Assert.AreEqual(events, this.Context.LoadAllEvents.Count);
        }
    }
}
