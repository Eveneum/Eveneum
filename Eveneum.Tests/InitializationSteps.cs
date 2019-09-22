using Eveneum.Tests.Infrastrature;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace Eveneum.Tests
{
    [Binding]
    public class InitializationSteps
    {
        private readonly CosmosDbContext Context;
        private readonly ScenarioContext ScenarioContext;

        InitializationSteps(CosmosDbContext context, ScenarioContext scenarioContext)
        {
            this.Context = context;
            this.ScenarioContext = scenarioContext;
        }

        [Then(@"the action fails as event store is not initialized")]
        public void ThenTheActionFailsAsEventStoreIsNotInitialized()
        {
            Assert.NotNull(this.ScenarioContext.TestError);
            Assert.IsInstanceOf<NotInitializedException>(this.ScenarioContext.TestError);
        }
    }
}
