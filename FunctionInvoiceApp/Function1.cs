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
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using Xero.NetStandard.OAuth2.Config;
using System.Net.Http;
using Xero.NetStandard.OAuth2.Client;
using Xero.NetStandard.OAuth2.Token;
using Xero.NetStandard.OAuth2.Model.Accounting;
using Xero.NetStandard.OAuth2.Token;
using Xero.NetStandard.OAuth2.Api;
using Xero.NetStandard.OAuth2.Config;
using Xero.NetStandard.OAuth2.Client;
using FunctionInvoiceApp.Utility;
using Microsoft.ApplicationInsights.DataContracts;
using FunctionInvoiceApp.Helper;
using System.Collections.Generic;

namespace FunctionInvoiceApp
{

    public class InvoiceTrigger
    {
        private readonly IOptions<WebhookSettings> _webhookSettings;
        private readonly IOptions<XeroConfiguration> _xeroConfig;
        private readonly TelemetryClient _telemetryClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TokenUtilities _tokenUtilities;
        public InvoiceTrigger(ILogger<InvoiceTrigger> log, IOptions<WebhookSettings> webHookSettings, IOptions<XeroConfiguration> xeroConfig, IHttpClientFactory httpClientFactory, 
            TelemetryClient telemetryClient, TokenUtilities tokenUtilities)
        {
            _telemetryClient = telemetryClient;
            _webhookSettings = webHookSettings;
            _xeroConfig = xeroConfig;
            _httpClientFactory = httpClientFactory;
            _tokenUtilities = tokenUtilities;
        }

        [FunctionName("Webhook")]
        public async Task<IActionResult> Webhook(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            _telemetryClient.TrackTrace("Webhook triggered!", SeverityLevel.Information);

            var reader = new StreamReader(req.Body);
            var payloadString = await reader.ReadToEndAsync();

            var signature = req.Headers[_webhookSettings.Value.XeroSignature].FirstOrDefault();

            if (!VerifySignature(payloadString, signature))
            {
                _telemetryClient.TrackTrace("Webhook 401", SeverityLevel.Critical);
                return new UnauthorizedResult();
            }

            var xeroToken = await _tokenUtilities.GetStoredToken();
            var utcTimeNow = DateTime.UtcNow;

            if (utcTimeNow > xeroToken.ExpiresAtUtc)
            {
                var client = new XeroClient(_xeroConfig.Value, _httpClientFactory.CreateClient());
                xeroToken = (XeroOAuth2Token)await client.RefreshAccessTokenAsync(xeroToken);

                await _tokenUtilities.StoreToken(xeroToken);

                _telemetryClient.TrackTrace($@"TokenRefresh successful. Token expired at {xeroToken.ExpiresAtUtc} UTC", SeverityLevel.Information);
            }

            var payload = JsonConvert.DeserializeObject<Payload>(payloadString);

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
                }
            }
            
            return new OkResult();
        }

        [FunctionName("TokenRefresh")]
        public async Task TokenRefresh([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer)
        {
            if (myTimer.IsPastDue)
            {
                _telemetryClient.TrackTrace("TokenRefresh timer is delay", SeverityLevel.Warning);
            }

            try {

                XeroOAuth2Token token = await _tokenUtilities.GetStoredToken();

                var httpClient = _httpClientFactory.CreateClient();
                var client = new XeroClient(_xeroConfig.Value, httpClient);

                XeroOAuth2Token updatedToken = (XeroOAuth2Token)await client.RefreshAccessTokenAsync(token);
                await _tokenUtilities.StoreToken(updatedToken);

                _telemetryClient.TrackTrace($@"TokenRefresh successful. Token expired at {updatedToken.ExpiresAtUtc} UTC", SeverityLevel.Information);
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
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
                bool isMatch = hashMsg == signature;

                _telemetryClient.TrackTrace($"Signature: {signature} HashMsg: {hashMsg} isMatch:{isMatch}");

                return isMatch;
            }
        }
    }
}
