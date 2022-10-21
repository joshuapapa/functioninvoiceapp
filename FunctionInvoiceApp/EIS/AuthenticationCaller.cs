using System;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace EIS
{
    public class AuthenticationCaller
    {
        private const string API_URL = "https://eis-cert.bir.gov.ph/api/authentication";
        private IHttpClientFactory _httpClientFactory;
        public AuthenticationCaller(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<SessionInfo> CallAPI()
        {
            try
            {
                string AUTH_KEY = "aaaaaaaaaabbbbbbbbbbcccccccccc12";

                JObject userInfo = new JObject
                {
                    { "userId", EisCredential.USER_ID },
                    { "password", EisCredential.PASSWORD },
                    { "authKey", AUTH_KEY }
                };

                JObject bodyInfo = new JObject
                {
                    { "data", GetEncryptedBase64(userInfo) },
                    { "forceRefreshToken", "false" }
                };

                var httpContent = new StringContent(bodyInfo.ToString());

                string datetime = DateTime.UtcNow.AddHours(8).ToString("yyyyMMddHHmmss");
                httpContent.Headers.Add("Authorization", GetHmacSignature(datetime));
                httpContent.Headers.Add("ApplicationId", EisCredential.APPLICATION_ID);
                httpContent.Headers.Add("AccreditationId", EisCredential.ACCREDITATION_ID);
                httpContent.Headers.Add("Datetime", datetime);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json; chearset=utf-8");

                HttpClient httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.PostAsync(API_URL, httpContent);

                string responseString = await response.Content.ReadAsStringAsync();

                JObject decryptJObject = GetDecryptedJson(
                    secretKey: AUTH_KEY,
                    encryptedString: JObject.Parse(responseString)["data"].ToString());

                Console.WriteLine("########### Decryptor Response Data");
                Console.WriteLine("accreditationId : " + decryptJObject["accreditationId"]);
                Console.WriteLine("userId : " + decryptJObject["userId"]);
                Console.WriteLine("authToken : " + decryptJObject["authToken"]);
                Console.WriteLine("sessionKey : " + decryptJObject["sessionKey"]);
                Console.WriteLine("tokenExpiry : " + decryptJObject["tokenExpiry"]);

                return new SessionInfo(
                    decryptJObject["authToken"].ToString(),
                    decryptJObject["sessionKey"].ToString());

            }
            catch (Exception ex)
            {
                Console.WriteLine("###exception###");
                Console.WriteLine(ex.Message);

                return null;
            }
        }

        private static string GetHmacSignature(string datetime)
        {
            string method = "POST";
            string hmacValue = datetime + method + "/api/authentication";

            string signature;
            using (var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(
                EisCredential.APPLICATION_SECRET_KEY)))
            {
                var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(hmacValue));
                signature = "Bearer " + Convert.ToBase64String(hash);
            }

            return signature;
        }

        private static string GetEncryptedBase64(JObject userInfo)
        {
            Asn1Object obj = Asn1Object.FromByteArray(Convert.FromBase64String(
                            EisCredential.PUBLIC_KEY));
            DerSequence publicKeySequence = (DerSequence)obj;

            DerBitString encodedPublicKey = (DerBitString)publicKeySequence[1];
            DerSequence publicKey = (DerSequence)Asn1Object.FromByteArray(
                encodedPublicKey.GetBytes());

            DerInteger modulus = (DerInteger)publicKey[0];
            DerInteger exponent = (DerInteger)publicKey[1];

            RsaKeyParameters keyParameters = new RsaKeyParameters(
                isPrivate: false,
                modulus.PositiveValue,
                exponent.PositiveValue);
            RSAParameters parameters = DotNetUtilities.ToRSAParameters(
                keyParameters);

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.ImportParameters(parameters);

            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(userInfo, Formatting.None));
            byte[] encryptedData = rsa.Encrypt(dataToEncrypt, false);
            var encryptedBase64 = Convert.ToBase64String(encryptedData);
            Console.WriteLine(encryptedBase64);

            return encryptedBase64;
        }

        private static JObject GetDecryptedJson(
            string secretKey,
            string encryptedString)
        {
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.Zeros;
            aes.Key = Encoding.UTF8.GetBytes(secretKey);
            aes.IV = Encoding.UTF8.GetBytes(secretKey.Substring(0, 16));

            var decrypt = aes.CreateDecryptor();
            byte[] xBuff = null;
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Write))
                {
                    byte[] xXml = Convert.FromBase64String(encryptedString);
                    // byte[] xXml = Convert.FromBase64String("kMBCxskWkYXgARlVJFT/96YqI4cr/jZaYv2T8pDs7DMTE8FWxkQQAt+KKY3lyp8P88kkRL5i5K3BExzzMHf00V92KlWU2e/Y9/Hg+EeCFvcx94TpEdnq1AUyinDSnnfHLMsuWcSXlavL2++oiUn3liGpie4XI8mInSQ0yWscmGpc4KfITqIOFbE3uABx/WpsapZVk6XVudKgpghnzoAAjHowV7sFtZirx4yRTY42ufEFneA91iEs/50SbQvuAGip");

                    cs.Write(xXml, 0, xXml.Length);
                }

                xBuff = ms.ToArray();
            }

            string decryptResult = Encoding.UTF8.GetString(xBuff).Trim().TrimEnd('\0').TrimEnd('\b');
            decryptResult = Regex.Replace(decryptResult, @"[^\u0000-\u007F]+", string.Empty);
            decryptResult = Regex.Replace(decryptResult, @"[^\t\r\n -~]", "");

            return JObject.Parse(decryptResult);
        }
    }
}
