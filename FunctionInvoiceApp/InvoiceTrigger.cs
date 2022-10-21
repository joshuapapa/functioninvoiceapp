using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FunctionInvoiceApp.Config;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Security.Cryptography;
using FunctionInvoiceApp.Dto;
using Microsoft.ApplicationInsights;
using Xero.NetStandard.OAuth2.Config;
using System.Net.Http;
using Xero.NetStandard.OAuth2.Client;
using Xero.NetStandard.OAuth2.Token;
using Xero.NetStandard.OAuth2.Model.Accounting;
using Xero.NetStandard.OAuth2.Api;
using FunctionInvoiceApp.Utility;
using Microsoft.ApplicationInsights.DataContracts;
using FunctionInvoiceApp.Helper;
using System.Collections.Generic;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.ApplicationInsights.Extensibility;

namespace FunctionInvoiceApp
{

    public class InvoiceTrigger
    {
        private readonly IOptions<WebhookSettings> _webhookSettings;
        private readonly IOptions<XeroConfiguration> _xeroConfig;
        private readonly TelemetryClient _telemetryClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TokenUtilities _tokenUtilities;
        private readonly QueueClient _queueClient;
        public InvoiceTrigger(IOptions<WebhookSettings> webHookSettings, IOptions<XeroConfiguration> xeroConfig, IHttpClientFactory httpClientFactory, 
            TokenUtilities tokenUtilities, QueueClient queueClient)
        {
            _webhookSettings = webHookSettings;
            _xeroConfig = xeroConfig;
            _httpClientFactory = httpClientFactory;
            _tokenUtilities = tokenUtilities;
            _queueClient = queueClient;
            _telemetryClient = TelemetryClientHelper.GetInstance();
        }

        [FunctionName("Webhook")]
        public async Task<IActionResult> Webhook(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            try
            {
                var reader = new StreamReader(req.Body);
                var payloadString = await reader.ReadToEndAsync();

                var signature = req.Headers[_webhookSettings.Value.XeroSignature].FirstOrDefault();

                if (!VerifySignature(payloadString, signature))
                {
                    _telemetryClient.TrackTrace("Webhook 401", SeverityLevel.Critical);
                    return new UnauthorizedResult();
                }

                _telemetryClient.TrackTrace("Sending webhook payload to queue", SeverityLevel.Information);
                //as per Xero, our API need to response less than 5 seconds so we need to process it separately

                await _queueClient.SendMessageAsync(payloadString); 
            }
            catch(Exception ex)
            {
                _telemetryClient.TrackException(ex);
            }

            return new OkResult();
        }

        [FunctionName("InvoiceQueueTrigger")]
        public async Task InvoiceQueueTrigger([QueueTrigger("invoicequeue", Connection = "AzureWebJobsStorage")] string queueItem)
        {
            var xeroToken = await _tokenUtilities.GetStoredToken();
            var utcTimeNow = DateTime.UtcNow;

            if (utcTimeNow > xeroToken.ExpiresAtUtc)
            {
                var client = new XeroClient(_xeroConfig.Value, _httpClientFactory.CreateClient());
                xeroToken = (XeroOAuth2Token)await client.RefreshAccessTokenAsync(xeroToken);

                await _tokenUtilities.StoreToken(xeroToken);

                _telemetryClient.TrackTrace($@"TokenRefresh successful. Token expired at {xeroToken.ExpiresAtUtc} UTC", SeverityLevel.Information);
            }

            var payload = JsonConvert.DeserializeObject<Payload>(queueItem);

            foreach (var payloadEvent in payload.Events)
            {
                var prop = payloadEvent.ToStringDictionary();
                _telemetryClient.TrackTrace("New Payload recieved", SeverityLevel.Information, prop);

                var AccountingApi = new AccountingApi();
                var invoiceIds = new List<Guid>
                {
                    payloadEvent.ResourceId
                };

                var response = await AccountingApi.GetInvoicesAsync(xeroToken.AccessToken, payloadEvent.TenantId.ToString(), null, null, null, invoiceIds);

                var invoices = response._Invoices;
                foreach (Invoice invoice in invoices)
                {
                    var invoiceProp = invoice.ToStringDictionary();
                    _telemetryClient.TrackTrace("New Invoice recieved", SeverityLevel.Information, invoiceProp);

                    foreach (LineItem lineItem in invoice.LineItems)
                    {
                        var lineItemProp = lineItem.ToStringDictionary();
                        _telemetryClient.TrackTrace($"LineItem of Invoice {invoice.InvoiceNumber}", SeverityLevel.Information, lineItemProp);

                        var itemProp = lineItem.Item.ToStringDictionary();
                        _telemetryClient.TrackTrace($"Item of Invoice {invoice.InvoiceNumber}", SeverityLevel.Information, itemProp);
                    }
                }
            }
        }

        private bool VerifySignature(string payload, string signature)
        {
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(_webhookSettings.Value.WebhookKey);
            byte[] payloadByte = encoding.GetBytes(payload);

            using (var hmac = new HMACSHA256(keyByte))
            {
                byte[] hashMessage = hmac.ComputeHash(payloadByte);
                var hashMsg = Convert.ToBase64String(hashMessage);
                return hashMsg == signature;
            }
        }
    }
}
