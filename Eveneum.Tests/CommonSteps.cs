using Eveneum.Tests.Infrastrature;
using System;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Eveneum.Tests
{
    [Binding]
    class CommonSteps
    {
        private readonly CosmosDbContext Context;

        public CommonSteps(CosmosDbContext context)
        {
            this.Context = context;
        }

        [Given(@"an event store backed by partitioned collection")]
        public async Task GivenAnEventStoreBackedByPartitionedCollection()
        {
            await this.Context.Initialize();
        }

        [Given(@"hard-delete mode")]
        public void GivenHardDeleteMode()
        {
            this.Context.EventStore.DeleteMode = DeleteMode.HardDelete;
        }

        [Given(@"an existing stream ([^\s-]) with (\d+) events")]
        public async Task GivenAnExistingStream(string streamId, ushort events)
        {
            ScenarioContext.Current.SetStreamId(streamId);

            await this.Context.EventStore.WriteToStream(streamId, TestSetup.GetEvents(events));
        }

        [Given(@"an existing stream ([^\s-]) with metadata and (\d+) events")]
        public async Task GivenAnExistingStreamWithMetadataAndEvents(string streamId, ushort events)
        {
            ScenarioContext.Current.SetStreamId(streamId);
            ScenarioContext.Current.SetHeaderMetadata(TestSetup.GetMetadata());

            await this.Context.EventStore.WriteToStream(streamId, TestSetup.GetEvents(events), metadata: ScenarioContext.Current.GetHeaderMetadata());
        }

        [Given(@"a deleted stream ([^\s-]) with (\d+) events")]
        public async Task GivenADeletedStream(string streamId, ushort events)
        {
            var eventData = TestSetup.GetEvents(events);

            await this.Context.EventStore.WriteToStream(streamId, eventData);
            await this.Context.EventStore.DeleteStream(streamId, (ulong)eventData.Length);
        }
    }
}
