using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FunctionInvoiceApp.Entity;
using FunctionInvoiceApp.Helper;
using Jose;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace EIS
{
    internal class InvoiceIssuanceCaller
    {
        private SessionInfo _sessionInfo;
        private string _submitId = string.Empty;
        private const string API_URL = "https://eis-cert.bir.gov.ph/api/invoices";

        private IHttpClientFactory _httpClientFactory;
        private readonly TelemetryClient _telemetryClient;
        public InvoiceIssuanceCaller(SessionInfo sessionInfo, IHttpClientFactory httpClientFactory)
        {
            _sessionInfo = sessionInfo;

            _httpClientFactory = httpClientFactory;
            _telemetryClient = TelemetryClientHelper.GetInstance();
        }

        public async Task<string> CallAPI(ElectronicInvoice eInvoice)
        {
            try
            {
                JObject jsonBodyRequest = new JObject
                {
                    { "submitId", GetSubmitId()},
                    { "data", GetEncryptedBase64(JsonConvert.SerializeObject(eInvoice))}
                };

                var httpContent = new StringContent(jsonBodyRequest.ToString());

                string datetime = DateTime.UtcNow.AddHours(8).ToString("yyyyMMddHHmmss");
                httpContent.Headers.Add("Authorization", GetHmacSignature(datetime));
                httpContent.Headers.Add("ApplicationId", EisCredential.APPLICATION_ID);
                httpContent.Headers.Add("AccreditationId", EisCredential.ACCREDITATION_ID);
                httpContent.Headers.Add("Datetime", datetime);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json; charset=utf-8");
                httpContent.Headers.Add("AuthToken", _sessionInfo.AuthenticationToken);

                HttpClient httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsync(API_URL, httpContent);

                string responseString = await response.Content.ReadAsStringAsync();

                JObject jsonResponse = JObject.Parse(responseString);
                if (jsonResponse["status"].ToString() == "1")
                {
                    JObject decryptData = GetDecryptedJson(responseString);

                    Dictionary<string, string> propDictionary = new Dictionary<string, string>();
                    propDictionary.Add("accreditationId", decryptData["accreditationId"].ToString());
                    propDictionary.Add("userId", decryptData["userId"].ToString());
                    propDictionary.Add("refSubmitId", decryptData["refSubmitId"].ToString());
                    propDictionary.Add("ackId", decryptData["ackId"].ToString());
                    propDictionary.Add("responseDtm", decryptData["responseDtm"].ToString());
                    propDictionary.Add("description", decryptData["description"].ToString());

                    _telemetryClient.TrackTrace("Decryptor Response Data", SeverityLevel.Information, propDictionary);

                    return _submitId;
                }
                else
                {
                    var errorMessage = jsonResponse["errorDetails"]["errorCode"].ToString() + ": " + jsonResponse["errorDetails"]["errorMessage"].ToString();
                    _telemetryClient.TrackException(new Exception(errorMessage));
                    return null;
                }

            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
                return null;
            }
        }

        private string GetHmacSignature(string datetime)
        {
            string method = "POST";
            string hmacValue = datetime + method + "/api/invoices";
            string signature;
            using (var hmacsha256 = new HMACSHA256(
                Encoding.UTF8.GetBytes(_sessionInfo.SessionSecretKey)))
            {
                var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(hmacValue));
                signature = "Bearer " + Convert.ToBase64String(hash);
            }
            Console.WriteLine("##2 check signature");
            Console.WriteLine(signature);
            return signature;
        }

        private string GetEncryptedBase64(string jws)
        {
            byte[] byteJws = Encoding.UTF8.GetBytes(jws);

            var aesSend = new RijndaelManaged
            {
                KeySize = 256,
                BlockSize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = Encoding.UTF8.GetBytes(_sessionInfo.SessionSecretKey),
                IV = Encoding.UTF8.GetBytes(_sessionInfo.SessionSecretKey.Substring(0, 16))
            };

            var encryptor = aesSend.CreateEncryptor();
            var encryptedBase64 = Convert.ToBase64String(
                encryptor.TransformFinalBlock(byteJws, 0, byteJws.Length));

            return encryptedBase64;
        }

        private static string GetJws(string jsonInvoiceStr)
        {
            var jwsHeader = new Dictionary<string, object>
            {
                { "kid", EisCredential.JWS_KEY_ID }
            };

            RsaPrivateCrtKeyParameters keyPair;
            var pr = new PemReader(new StringReader(EisCredential.JWS_PRIVATE_KEY));
            keyPair = (RsaPrivateCrtKeyParameters)pr.ReadObject();

            RSAParameters rsaPparameters = DotNetUtilities.ToRSAParameters(keyPair);

            var rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(rsaPparameters);
            
            string jws = JWT.Encode(
                payload: jsonInvoiceStr, // Encoding.UTF8.GetBytes(jsonInvoiceStr),
                key: rsa,
                algorithm: JwsAlgorithm.RS256,
                extraHeaders: jwsHeader);

            return jws;
        }

        private JObject GetDecryptedJson(string responseString)
        {
            JObject responseJson = JObject.Parse(responseString);

            var aes = new RijndaelManaged
            {
                KeySize = 256,
                BlockSize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.Zeros,
                Key = Encoding.UTF8.GetBytes(_sessionInfo.SessionSecretKey),
                IV = Encoding.UTF8.GetBytes(_sessionInfo.SessionSecretKey.Substring(0, 16))
            };

            var decryptor = aes.CreateDecryptor();
            byte[] xBuff = null;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
                {
                    byte[] cb = Convert.FromBase64String(responseJson["data"].ToString());
                    cs.Write(cb, 0, cb.Length);
                }

                xBuff = ms.ToArray();
            }

            String decryptResult = Encoding.UTF8.GetString(xBuff).Trim();
            decryptResult = Regex.Replace(decryptResult, @"[^\t\r\n -~]", "");
            Console.WriteLine(decryptResult);
            JObject decryptJson = JObject.Parse(decryptResult);
            return decryptJson;
        }

        private string GetSubmitId()
        {
            var ran = new Random();
            string[] uniqueCharsList = "0,1,2,3,4,5,6,7,8,9,a,b,c,d,e,f".Split(',');
            string randResult = "";
            for (int i = 0; i < 12; i++)
            {
                randResult += uniqueCharsList[ran.Next(16)];
            }

            return EisCredential.ACCREDITATION_ID
                + "-"
                + DateTime.Now.ToString("yyyyMMdd")
                + "-"
                + randResult;
        }
    }
}