using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace HttpServer.asp.Services
{
    public class SignedUrlService
    {
        private readonly string _secretKey;
        private readonly TimeSpan _defaultExpiry;

        public SignedUrlService(string secretKey, TimeSpan? defaultExpiry = null)
        {
            _secretKey = secretKey ?? throw new ArgumentNullException(nameof(secretKey));
            _defaultExpiry = defaultExpiry ?? TimeSpan.FromMinutes(10); // default 10 min
        }

        /// <summary>
        /// Generates a signed URL for a given userId and filename
        /// </summary>
        public string GenerateSignedUrl(int userId, string filename, string baseUrl)
        {
            var expires = DateTimeOffset.UtcNow.Add(_defaultExpiry).ToUnixTimeSeconds();
            var signature = ComputeSignature(userId, filename, expires);

            // Build URL with query parameters
            var query = new System.Collections.Generic.Dictionary<string, string?>
            {
                ["expires"] = expires.ToString(),
                ["sig"] = signature
            };

            var url = QueryHelpers.AddQueryString($"{baseUrl}/{userId}/{Uri.EscapeDataString(filename)}", query);
            return url;
        }

        /// <summary>
        /// Validates a signed URL parameters
        /// </summary>
        public bool ValidateSignature(int userId, string filename, long expires, string signature)
        {
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expires)
                return false; // expired

            var expectedSig = ComputeSignature(userId, filename, expires);
            return expectedSig == signature;
        }

        private string ComputeSignature(int userId, string filename, long expires)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
            var data = $"{userId}/{filename}{expires}";
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash); // C# 10+; for older versions use BitConverter
        }
    }
}


