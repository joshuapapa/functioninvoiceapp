using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights;
using Xero.NetStandard.OAuth2.Config;
using System.Net.Http;
using Xero.NetStandard.OAuth2.Client;
using Xero.NetStandard.OAuth2.Token;
using FunctionInvoiceApp.Utility;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using FunctionInvoiceApp.Helper;

namespace FunctionInvoiceApp
{

    public class TokenTrigger
    {
        private readonly IOptions<XeroConfiguration> _xeroConfig;
        private readonly TelemetryClient _telemetryClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TokenUtilities _tokenUtilities;
        public TokenTrigger(IOptions<XeroConfiguration> xeroConfig, IHttpClientFactory httpClientFactory, 
            TokenUtilities tokenUtilities)
        {
            _xeroConfig = xeroConfig;
            _httpClientFactory = httpClientFactory;
            _tokenUtilities = tokenUtilities;
            _telemetryClient = TelemetryClientHelper.GetInstance();
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
    }
}
