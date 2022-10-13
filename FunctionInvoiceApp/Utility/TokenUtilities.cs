using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xero.NetStandard.OAuth2.Token;

namespace FunctionInvoiceApp.Utility
{
    public class TokenUtilities
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly BlobClient _blobClient;
        private readonly JsonSerializerOptions jsonSerializerOptions = new() { WriteIndented = true };

        public TokenUtilities()
        {
            _blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            _blobContainerClient = _blobServiceClient.GetBlobContainerClient("invoiceapp");
            _blobContainerClient.CreateIfNotExists();
            _blobClient = _blobContainerClient.GetBlobClient("xerotoken.json");
        }
        public async Task StoreToken(XeroOAuth2Token xeroToken)
        {
            string serializedXeroToken = JsonSerializer.Serialize(xeroToken, jsonSerializerOptions);

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(serializedXeroToken)))
            {
                await _blobClient.UploadAsync(ms, overwrite: true);
            }
        }

        public async Task<XeroOAuth2Token> GetStoredToken()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                await _blobClient.DownloadToAsync(stream);
                stream.Position = 0;//resetting stream's position to 0
                var xeroToken = JsonSerializer.Deserialize<XeroOAuth2Token>(stream, jsonSerializerOptions);
                return xeroToken;
            }
        }
    }
}
