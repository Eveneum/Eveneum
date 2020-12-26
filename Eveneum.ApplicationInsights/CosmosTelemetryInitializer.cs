using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Cosmos;
using System;

namespace Eveneum.ApplicationInsights
{
    public class CosmosTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            if(telemetry is ExceptionTelemetry)
            {
                var exceptionTelemetry = telemetry as ExceptionTelemetry;

                if(exceptionTelemetry.Exception is CosmosException)
                {
                    var cosmosException = exceptionTelemetry.Exception as CosmosException;

                    exceptionTelemetry.Properties[nameof(CosmosException.StatusCode)] = Convert.ToString(cosmosException.StatusCode);
                    exceptionTelemetry.Properties[nameof(CosmosException.SubStatusCode)] = Convert.ToString(cosmosException.SubStatusCode);
                    exceptionTelemetry.Properties[nameof(CosmosException.ActivityId)] = Convert.ToString(cosmosException.ActivityId);
                    exceptionTelemetry.Properties[nameof(CosmosException.RetryAfter)] = Convert.ToString(cosmosException.RetryAfter);
                    exceptionTelemetry.Properties[nameof(CosmosException.ResponseBody)] = cosmosException.ResponseBody;
                    exceptionTelemetry.Properties[nameof(CosmosException.RequestCharge)] = Convert.ToString(cosmosException.RequestCharge);
                    exceptionTelemetry.Properties["ClientElapsedTime"] = Convert.ToString(cosmosException?.Diagnostics.GetClientElapsedTime());
                    exceptionTelemetry.Properties[nameof(CosmosException.Headers.Session)] = cosmosException?.Headers.Session;
                    exceptionTelemetry.Properties[nameof(CosmosException.Headers.ETag)] = cosmosException?.Headers.ETag;
                    exceptionTelemetry.Properties[nameof(CosmosException.Headers.ContinuationToken)] = cosmosException?.Headers.ContinuationToken;
                    exceptionTelemetry.Properties[nameof(CosmosException.Headers.Location)] = cosmosException?.Headers.Location;
                }
            }
        }
    }
}
