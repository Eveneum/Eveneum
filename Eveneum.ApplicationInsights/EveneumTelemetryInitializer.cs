using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Globalization;

namespace Eveneum.ApplicationInsights
{
    public class EveneumTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            if(telemetry is ExceptionTelemetry)
            {
                var exceptionTelemetry = telemetry as ExceptionTelemetry;

                switch(exceptionTelemetry.Exception)
                {
                    case JsonDeserializationException ex:
                        exceptionTelemetry.Properties[nameof(ex.Type)] = ex.Type;
                        exceptionTelemetry.Properties[nameof(ex.Json)] = ex.Json;
                        break;

                    case OptimisticConcurrencyException ex:
                        exceptionTelemetry.Properties[nameof(ex.StreamId)] = ex.StreamId;
                        exceptionTelemetry.Properties[nameof(ex.RequestCharge)] = ex.RequestCharge.ToString("N", CultureInfo.InvariantCulture);
                        exceptionTelemetry.Properties[nameof(ex.ExpectedVersion)] = ex.ExpectedVersion.ToString();
                        exceptionTelemetry.Properties[nameof(ex.ActualVersion)] = ex.ActualVersion.ToString();
                        break;

                    case StreamAlreadyExistsException ex:
                        exceptionTelemetry.Properties[nameof(ex.StreamId)] = ex.StreamId;
                        exceptionTelemetry.Properties[nameof(ex.RequestCharge)] = ex.RequestCharge.ToString("N", CultureInfo.InvariantCulture);
                        break;

                    case StreamDeletedException ex:
                        exceptionTelemetry.Properties[nameof(ex.StreamId)] = ex.StreamId;
                        exceptionTelemetry.Properties[nameof(ex.RequestCharge)] = ex.RequestCharge.ToString("N", CultureInfo.InvariantCulture);
                        break;

                    case StreamDeserializationException ex:
                        exceptionTelemetry.Properties[nameof(ex.StreamId)] = ex.StreamId;
                        exceptionTelemetry.Properties[nameof(ex.RequestCharge)] = ex.RequestCharge.ToString("N", CultureInfo.InvariantCulture);
                        exceptionTelemetry.Properties[nameof(ex.Type)] = ex.Type;
                        break;

                    case StreamNotFoundException ex:
                        exceptionTelemetry.Properties[nameof(ex.StreamId)] = ex.StreamId;
                        exceptionTelemetry.Properties[nameof(ex.RequestCharge)] = ex.RequestCharge.ToString("N", CultureInfo.InvariantCulture);
                        break;

                    case TypeNotFoundException ex:
                        exceptionTelemetry.Properties[nameof(ex.Type)] = ex.Type;
                        break;

                    case WriteException ex:
                        exceptionTelemetry.Properties[nameof(ex.StreamId)] = ex.StreamId;
                        exceptionTelemetry.Properties[nameof(ex.RequestCharge)] = ex.RequestCharge.ToString("N", CultureInfo.InvariantCulture);
                        exceptionTelemetry.Properties[nameof(ex.StatusCode)] = ex.StatusCode.ToString();
                        break;

                    default:
                        break;
                }
            }
        }
    }
}
