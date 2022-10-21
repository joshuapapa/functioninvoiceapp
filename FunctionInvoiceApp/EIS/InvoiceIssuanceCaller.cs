using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Jose;
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

        public InvoiceIssuanceCaller(SessionInfo sessionInfo)
        {
            _sessionInfo = sessionInfo;
        }

        public string CallAPI()
        {
            DateTime utcTime = DateTime.UtcNow;
            DateTime philippinesTime = utcTime.AddHours(8);
            string datetime = philippinesTime.ToString("yyyyMMddHHmmss");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(API_URL);
            request.Method = "POST";
            request.Headers.Add("Authorization", GetHmacSignature(datetime));
            request.Headers.Add("ApplicationId", EisCredential.APPLICATION_ID);
            request.Headers.Add("AccreditationId", EisCredential.ACCREDITATION_ID);
            request.Headers.Add("Datetime", datetime);
            request.Headers.Add("Content-Type", "application/json; chearset=utf-8");
            request.Headers.Add("AuthToken", _sessionInfo.AuthenticationToken);

            StreamReader reader = new StreamReader("../../../Sample_CAS_invoice.json");
            String invoiceJsonString = reader.ReadToEnd();
            reader.Close();

            var bodyJson = new JObject();
            bodyJson.Add("submitId", GetSubmitId());
            bodyJson.Add("data", GetEncryptedBase64(GetJws(invoiceJsonString)));

            var reqStream = new StreamWriter(request.GetRequestStream());
            reqStream.Write(JsonConvert.SerializeObject(bodyJson, Formatting.None));
            reqStream.Close();

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader respStream = new StreamReader(response.GetResponseStream());
                string responseString = respStream.ReadToEnd();
                respStream.Close();
                Console.WriteLine("########### Response Data");
                Console.WriteLine(responseString);

                JObject decryptJson = GetDecryptedJson(responseString);

                Console.WriteLine("##8 Decryptor Response Data");
                Console.WriteLine("accreditationId : " + decryptJson["accreditationId"]);
                Console.WriteLine("userId : " + decryptJson["userId"]);
                Console.WriteLine("refSubmitId : " + decryptJson["refSubmitId"]);
                Console.WriteLine("ackId : " + decryptJson["ackId"]);
                Console.WriteLine("responseDtm : " + decryptJson["responseDtm"]);
                Console.WriteLine("description : " + decryptJson["description"]);

                return _submitId;
            }
            catch (Exception ex)
            {
                Console.WriteLine("###exception###");
                Console.WriteLine(ex.Message);

                return string.Empty;
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