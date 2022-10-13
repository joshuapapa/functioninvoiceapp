using FunctionInvoiceApp.Config;
using FunctionInvoiceApp.Utility;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Xero.NetStandard.OAuth2.Config;

[assembly: FunctionsStartup(typeof(MyNamespace.Startup))]

namespace MyNamespace
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<WebhookSettings>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("WebhookSettings").Bind(settings);
                });

            builder.Services.AddOptions<XeroConfiguration>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("XeroConfiguration").Bind(settings);
                });

            builder.Services.AddHttpClient();

            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
            var telemetryClient = new TelemetryClient(telemetryConfiguration);

            builder.Services.AddSingleton<TelemetryClient>(telemetryClient);
            builder.Services.AddSingleton<TokenUtilities>(new TokenUtilities());
            /*
            builder.Services.AddSingleton<IMyService>((s) => {
                return new MyService();
            });

            builder.Services.AddSingleton<ILoggerProvider, Logger>();
            */
        }
    }
}