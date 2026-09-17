using Eveneum.Tests.Infrastructure;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Reqnroll;

namespace Eveneum.Tests
{
    [Binding]
    public class CommonSteps(ScenarioContext scenarioContext, IEnumerable<CosmosDbContext> Contexts)
    {
        [Given(@"Cosmos serializer with camel-case naming policy")]
        public void GivenCosmosSerializerWithCamelCaseNamingPolicy()
        {
            foreach (var context in Contexts)
            {
                switch (context)
                {
                    case NewtonsoftCosmosDbContext newtonsoftCosmosDbContext:
                        var contractResolver = new CamelCasePropertyNamesContractResolver();
                        contractResolver.NamingStrategy.OverrideSpecifiedNames = false;

                        newtonsoftCosmosDbContext.JsonSerializerSettings.ContractResolver = contractResolver;
                        break;

                    case SystemTextJsonCosmosDbContext systemTextJsonCosmosDbContext:
                        systemTextJsonCosmosDbContext.JsonSerializerOptions = new System.Text.Json.JsonSerializerOptions
                        {
                            IncludeFields = true,
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                        };
                        break;
                    
                    default:
                        break;
                }
            }
        }

        [Given("an event store")]
        public async Task GivenAnEventStore()
        {
            await Task.WhenAll(Contexts.Select(x => x.Initialize()));
        }

        [Given("hard-delete mode")]
        public void GivenHardDeleteMode()
        {
            foreach (var context in Contexts)
            {
                context.EventStoreOptions.DeleteMode = DeleteMode.HardDelete;
            }
        }

        [Given("ttl-delete mode with {int} seconds as ttl")]
        public void GivenTTlDeleteMode(int streamTtlAfterDelete)
        {
            foreach (var context in Contexts)
            {
                context.EventStoreOptions.DeleteMode = DeleteMode.TtlDelete;
                context.EventStoreOptions.StreamTimeToLiveAfterDelete = TimeSpan.FromSeconds(streamTtlAfterDelete);
            }
        }

        [Given("single snapshot mode")]
        public void GivenSingleSnapshotMode()
        {
            foreach (var context in Contexts)
            {
                context.EventStoreOptions.SnapshotMode = SnapshotMode.Single;
            }
        }

        [Given("a batch size of {int}")]
        public void GivenABatchSize(int batchSize)
        {
            foreach (var context in Contexts)
            {
                context.EventStoreOptions.BatchSize = (byte)batchSize;
            }
        }

        [Given("an existing stream {word} with {int} events")]
        public async Task GivenAnExistingStream(string streamId, ushort events)
        {
            var eventData = TestSetup.GetEvents(events);

            await Task.WhenAll(Contexts.Select(async x =>
            {
                x.StreamId = streamId;

                await x.EventStore.WriteToStream(streamId, eventData);
            }));
        }

        [Given("an existing stream {word} with metadata and {int} events")]
        public async Task GivenAnExistingStreamWithMetadataAndEvents(string streamId, ushort events)
        {
            var metadata = TestSetup.GetMetadata();
            var eventData = TestSetup.GetEvents(events);

            await Task.WhenAll(Contexts.Select(async x =>
            {
                x.StreamId = streamId;
                x.HeaderMetadata = metadata;

                await x.EventStore.WriteToStream(streamId, eventData, metadata: metadata);
            }));
        }

        [Given("a deleted stream {word} with {int} events")]
        public async Task GivenADeletedStream(string streamId, ushort events)
        {
            var eventData = TestSetup.GetEvents(events);

            await Task.WhenAll(Contexts.Select(async x =>
            {
                x.StreamId = streamId;

                await x.EventStore.WriteToStream(streamId, eventData);
                await x.EventStore.DeleteStream(streamId, (ulong)eventData.Length);
            }));
        }

        [When("I wait for {int} seconds")]
        public async Task Wait(int waitForSeconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(waitForSeconds));
        }

        [Then("request charge is reported")]
        public void ThenRequestChargeIsReported()
        {
            foreach (var context in Contexts)
            {
                var requestCharge = scenarioContext.TestError is EveneumException
                    ? (scenarioContext.TestError as EveneumException).RequestCharge
                    : context.Response.RequestCharge;

                Console.WriteLine($"Request charge ({context.GetType().Name}): {requestCharge}");

                Assert.That(requestCharge, Is.GreaterThan(0));
            }
        }

        [Then("{int} deleted documents are reported")]
        public void ThenDeletedDocumentsAreReported(ulong deletedDocuments)
        {
            foreach (var context in Contexts)
            {
                Assert.That(context.Response, Is.InstanceOf<DeleteResponse>());

                var response = context.Response as DeleteResponse;

                Assert.That(response.DeletedDocuments, Is.EqualTo(deletedDocuments));
            }
        }
    }
}