using Eveneum.Tests.Infrastructure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Reqnroll;

namespace Eveneum.Tests
{
    [Binding]
    public class InvalidBatchSizeSteps(IEnumerable<CosmosDbContext> Contexts)
    {
        [When("I create an event store")]
        public async Task WhenICreateAnEventStore()
        {
            await Task.WhenAll(Contexts.Select(async context =>
            {
                try
                {
                    await context.Initialize();
                }
                catch (Exception ex)
                {
                    context.Exception = ex;
                }
            }));
        }

        [Then("the action fails as the batch size must be greater than zero")]
        public void ThenTheActionFailsAsTheBatchSizeMustBeGreaterThanZero()
        {
            foreach (var context in Contexts)
            {
                Assert.That(context.Exception, Is.InstanceOf<ArgumentOutOfRangeException>());

                var exception = context.Exception as ArgumentOutOfRangeException;
                Assert.That(exception.ParamName, Is.EqualTo("options"));
            }
        }
    }
}
