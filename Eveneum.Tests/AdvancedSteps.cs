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

            ScenarioContext.Current.Set(events, "LoadAllEvents");
        }

        [Then(@"all (\d+) events are loaded")]
        public void ThenAllEventsAreLoaded(ulong events)
        {
            var loadedEvents = ScenarioContext.Current.Get<List<EventData>>("LoadAllEvents");

            Assert.AreEqual(events, loadedEvents.Count);
        }
    }
}
