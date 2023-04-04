using System.Reflection;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;

namespace Eveneum.Tests.Infrastructure
{
    [Binding]
    public class StepArgumentConversions
    {
        private readonly ScenarioContext ScenarioContext;

        StepArgumentConversions(ScenarioContext scenarioContext)
        {
            this.ScenarioContext = scenarioContext;
        }

        [AfterStep("ExpectException")]
        public void ExpectException()
        {
            if (this.ScenarioContext.StepContext.StepInfo.StepDefinitionType == StepDefinitionType.When)
            {
                PropertyInfo testStatusProperty = typeof(ScenarioContext).GetProperty(nameof(this.ScenarioContext.ScenarioExecutionStatus), BindingFlags.Public | BindingFlags.Instance);
                testStatusProperty.SetValue(this.ScenarioContext, ScenarioExecutionStatus.OK);
            }
        }
    }
}
