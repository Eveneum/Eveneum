using Eveneum.Serialization;
using Eveneum.Tests.Infrastructure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Reqnroll;

namespace Eveneum.Tests
{
    public class UnresolvableTypeProvider : ITypeProvider
    {
        private readonly PlatformTypeProvider Resolver = new();

        public string GetIdentifierForType(Type type) => $"{type.FullName}, MissingAssembly";

        public Type GetTypeForIdentifier(string identifier) => this.Resolver.GetTypeForIdentifier(identifier);
    }

    [Binding]
    [Scope(Feature = "Reading stream with missing types")]
    public class ReadingStreamWithMissingTypesSteps(ScenarioContext scenarioContext, IEnumerable<CosmosDbContext> Contexts)
    {
        [Given("a type provider returning unresolvable type identifiers")]
        public void GivenATypeProviderReturningUnresolvableTypeIdentifiers()
        {
            foreach (var context in Contexts)
                context.EventStoreOptions.TypeProvider = new UnresolvableTypeProvider();
        }

        [Given("an event store that ignores missing types")]
        public void GivenAnEventStoreThatIgnoresMissingTypes()
        {
            foreach (var context in Contexts)
                context.EventStoreOptions.IgnoreMissingTypes = true;
        }

        [Then("the action fails to read stream {word} because a type wasn't found")]
        public void ThenTheActionFailsToReadStreamBecauseATypeWasntFound(string streamId)
        {
            Assert.That(scenarioContext.TestError, Is.InstanceOf<StreamDeserializationException>());

            var exception = scenarioContext.TestError as StreamDeserializationException;
            Assert.That(exception.StreamId, Is.EqualTo(streamId));
            Assert.That(exception.Type, Is.EqualTo(new UnresolvableTypeProvider().GetIdentifierForType(typeof(SampleMetadata))));
        }

        [Then("all events are returned without body or metadata")]
        public void ThenAllEventsAreReturnedWithoutBodyOrMetadata()
        {
            foreach (var context in Contexts)
            {
                Assert.That(context.Stream.HasValue);
                Assert.That(context.Stream.Value.Events, Is.Not.Empty);

                foreach (var @event in context.Stream.Value.Events)
                {
                    Assert.That(@event.Body, Is.Null);
                    Assert.That(@event.Metadata, Is.Null);
                }
            }
        }
    }
}
