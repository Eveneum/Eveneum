using System;
using System.Reflection;
using Reqnroll;
using Reqnroll.Bindings;

namespace Eveneum.Tests.Infrastructure;

[Binding]
public class StepArgumentConversions(ScenarioContext ScenarioContext)
{
    [AfterStep("ExpectException")]
    public void ExpectException()
    {
        if (ScenarioContext.StepContext.StepInfo.StepDefinitionType == StepDefinitionType.When)
        {
            var testStatusProperty = typeof(ScenarioContext).GetProperty(nameof(ScenarioContext.ScenarioExecutionStatus), BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMemberException(nameof(ScenarioContext), nameof(ScenarioContext.ScenarioExecutionStatus));
            testStatusProperty.SetValue(ScenarioContext, ScenarioExecutionStatus.OK);
        }
    }
}
