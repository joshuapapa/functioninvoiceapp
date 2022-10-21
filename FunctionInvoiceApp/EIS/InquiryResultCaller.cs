using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace EIS
{
    internal class InquiryResultCaller
    {
        private SessionInfo _sessionInfo;
        private string _submitId;
        private string _apiUrl;

        public InquiryResultCaller(SessionInfo sessionInfo, string submitId)
        {
            _sessionInfo = sessionInfo;
            _submitId = submitId;
            _apiUrl = "https://eis-cert.bir.gov.ph/api/invoice_result/" + submitId;
        }

        internal void CallAPI()
        {
            DateTime utcTime = DateTime.UtcNow;
            DateTime philippinesTime = utcTime.AddHours(8);
            string datetime = philippinesTime.ToString("yyyyMMddHHmmss");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_apiUrl);
            request.Method = "GET";
            request.Headers.Add("Authorization", GetHmacSignature(datetime));
            request.Headers.Add("ApplicationId", EisCredential.APPLICATION_ID);
            request.Headers.Add("AccreditationId", EisCredential.ACCREDITATION_ID);
            request.Headers.Add("Datetime", datetime);
            request.Headers.Add("Content-Type", "application/json; chearset=utf-8");
            request.Headers.Add("AuthToken", _sessionInfo.AuthenticationToken);

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader respStream = new StreamReader(response.GetResponseStream());
                string responseString = respStream.ReadToEnd();
                respStream.Close();
                Console.WriteLine("########### Response Data");
                Console.WriteLine(responseString);
            }
            catch (Exception ex)
            {
                Console.WriteLine("###exception###");
                Console.WriteLine(ex.Message);
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
                signature = "Bearer " + Convert.ToBase64String(hash);
            }
            Console.WriteLine("######## check signature");
            Console.WriteLine(signature);
            return signature;
        }
    }
}