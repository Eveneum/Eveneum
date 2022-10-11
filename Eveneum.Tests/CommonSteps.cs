using Eveneum.Tests.Infrastrature;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Eveneum.Tests
{
    [Binding]
    class CommonSteps
    {
        private readonly CosmosDbContext Context;
        private readonly ScenarioContext ScenarioContext;

        public CommonSteps(CosmosDbContext context, ScenarioContext scenarioContext)
        {
            this.Context = context;
            this.ScenarioContext = scenarioContext;
        }

        [Given(@"an event store backed by partitioned collection")]
        public async Task GivenAnEventStoreBackedByPartitionedCollection()
        {
            await this.Context.Initialize();
        }

        [Given(@"an uninitialized event store backed by partitioned collection")]
        public async Task GivenAnUninitializedEventStoreBackedByPartitionedCollection()
        {
            await this.Context.Initialize(false);
        }

        [Given(@"hard-delete mode")]
        public void GivenHardDeleteMode()
        {
            this.Context.EventStoreOptions.DeleteMode = DeleteMode.HardDelete;
        }

        [Given(@"ttl-delete mode with (\d+) seconds as ttl")]
        public void GivenTTlDeleteMode(int streamTtlAfterDelete)
        {
            this.Context.EventStoreOptions.DeleteMode = DeleteMode.TtlDelete;
            this.Context.EventStoreOptions.StreamTimeToLiveSecondsAfterDelete = TimeSpan.FromSeconds(streamTtlAfterDelete);
        }

        [Given(@"an existing stream ([^\s-]) with (\d+) events")]
        public async Task GivenAnExistingStream(string streamId, ushort events)
        {
            this.Context.StreamId = streamId;

            await this.Context.EventStore.WriteToStream(streamId, TestSetup.GetEvents(events));
        }

        [Given(@"an existing stream ([^\s-]) with metadata and (\d+) events")]
        public async Task GivenAnExistingStreamWithMetadataAndEvents(string streamId, ushort events)
        {
            this.Context.StreamId = streamId;
            this.Context.HeaderMetadata = TestSetup.GetMetadata();

            await this.Context.EventStore.WriteToStream(streamId, TestSetup.GetEvents(events), metadata: this.Context.HeaderMetadata);
        }

        [Given(@"a deleted stream ([^\s-]) with (\d+) events")]
        public async Task GivenADeletedStream(string streamId, ushort events)
        {
            this.Context.StreamId = streamId;
            var eventData = TestSetup.GetEvents(events);

            await this.Context.EventStore.WriteToStream(streamId, eventData);
            await this.Context.EventStore.DeleteStream(streamId, (ulong)eventData.Length);
        }

        [Then(@"request charge is reported")]
        public void ThenRequestChargeIsReported()
        {
            var requestCharge = this.ScenarioContext.TestError is EveneumException
                ? (this.ScenarioContext.TestError as EveneumException).RequestCharge
                : this.Context.Response.RequestCharge;

            Console.WriteLine("Request charge: " + requestCharge);

            Assert.Greater(requestCharge, 0);
        }

        [Then(@"(\d+) deleted documents are reported")]
        public void ThenDeletedDocumentsAreReported(ulong deletedDocuments)
        {
            Assert.IsInstanceOf<DeleteResponse>(this.Context.Response);

            var response = this.Context.Response as DeleteResponse;

            Assert.AreEqual(deletedDocuments, response.DeletedDocuments);
        }
    }
}