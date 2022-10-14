using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;

namespace FunctionInvoiceApp.Helper
{
    public static class TelemetryClientHelper
    {
        private static TelemetryClient _telemetryClient;
        public static TelemetryClient GetInstance()
        {
            if(_telemetryClient == null)
            {
                var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
                telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
                _telemetryClient = new TelemetryClient(telemetryConfiguration);
            }
            
            return _telemetryClient;
        }
    }
}
