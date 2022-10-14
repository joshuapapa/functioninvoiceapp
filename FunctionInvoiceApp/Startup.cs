using Azure.Storage.Queues;
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
            builder.Services.AddSingleton<TokenUtilities>(new TokenUtilities());

            var queueClient = new QueueClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "invoicequeue");
            queueClient.CreateIfNotExists();
            builder.Services.AddSingleton<QueueClient>(queueClient);
        }
    }
}