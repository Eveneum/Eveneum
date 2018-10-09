using System.Reflection;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;

namespace Eveneum.Tests.Infrastrature
{
    [Binding]
    public class StepArgumentConversions
    {
        [AfterStep("ExpectException")]
        public void ExpectException()
        {
            if (ScenarioContext.Current.StepContext.StepInfo.StepDefinitionType == StepDefinitionType.When)
            {
                PropertyInfo testStatusProperty = typeof(ScenarioContext).GetProperty(nameof(ScenarioContext.Current.ScenarioExecutionStatus), BindingFlags.Public | BindingFlags.Instance);
                testStatusProperty.SetValue(ScenarioContext.Current, ScenarioExecutionStatus.OK);
            }
        }
    }
}
