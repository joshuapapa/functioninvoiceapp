using FunctionInvoiceApp.Helper;
using Microsoft.ApplicationInsights;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EIS
{
    public class InquiryResultCaller
    {
        private SessionInfo _sessionInfo;
        private string _submitId;
        private string _apiUrl;

        private IHttpClientFactory _httpClientFactory;
        private readonly TelemetryClient _telemetryClient;
        private readonly AuthenticationCaller _authCaller;
        public InquiryResultCaller(IHttpClientFactory httpClientFactory, AuthenticationCaller authCaller)
        {
            _httpClientFactory = httpClientFactory;
            _telemetryClient = TelemetryClientHelper.GetInstance();
            _authCaller = authCaller;
        }

        public async Task CallAPI(string submitId)
        {
            try
            {
                _sessionInfo = await _authCaller.GetSession();

                _submitId = submitId;
                _apiUrl = "https://eis-cert.bir.gov.ph/api/invoice_result/" + submitId;

                HttpClient httpClient = _httpClientFactory.CreateClient();
                string datetime = DateTime.UtcNow.AddHours(8).ToString("yyyyMMddHHmmss");

                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, _apiUrl))
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetHmacSignature(datetime));
                    requestMessage.Headers.Add("Authorization", GetHmacSignature(datetime));
                    requestMessage.Headers.Add("ApplicationId", EisCredential.APPLICATION_ID);
                    requestMessage.Headers.Add("AccreditationId", EisCredential.ACCREDITATION_ID);
                    requestMessage.Headers.Add("Datetime", datetime);
                    requestMessage.Headers.Add("Content-Type", "application/json; charset=utf-8");
                    requestMessage.Headers.Add("AuthToken", _sessionInfo.AuthenticationToken);

                    var response = await httpClient.SendAsync(requestMessage);

                    string responseString = await response.Content.ReadAsStringAsync();

                    _telemetryClient.TrackTrace(responseString);
                }
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
            }
        }

        private string GetHmacSignature(string datetime)
        {
            string method = "GET";
            string hmacValue = datetime + method + "/api/invoice_result/" + _submitId;
            string signature;
            using (var hmacsha256 = new HMACSHA256(
                Encoding.UTF8.GetBytes(_sessionInfo.SessionSecretKey)))
            {
                var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(hmacValue));
                signature = Convert.ToBase64String(hash);
            }
            Console.WriteLine("######## check signature");
            Console.WriteLine(signature);
            return signature;
        }
    }
}