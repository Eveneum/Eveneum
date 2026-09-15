using System.Reflection;
using Reqnroll;
using Reqnroll.Bindings;

namespace Eveneum.Tests.Infrastructure
{
    [Binding]
    public class StepArgumentConversions(ScenarioContext ScenarioContext)
    {
        [AfterStep("ExpectException")]
        public void ExpectException()
        {
            if (ScenarioContext.StepContext.StepInfo.StepDefinitionType == StepDefinitionType.When)
            {
                PropertyInfo testStatusProperty = typeof(ScenarioContext).GetProperty(nameof(ScenarioContext.ScenarioExecutionStatus), BindingFlags.Public | BindingFlags.Instance);
                testStatusProperty.SetValue(ScenarioContext, ScenarioExecutionStatus.OK);
            }
        }
    }
}
