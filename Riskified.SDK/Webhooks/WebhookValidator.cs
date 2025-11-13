using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Riskified.SDK.Exceptions;
using Riskified.SDK.Model;
using Riskified.SDK.Model.Internal;

namespace Riskified.SDK.Webhooks
{
    /// <summary>
    /// Utility for validating and parsing Riskified webhooks in ASP.NET Core applications
    /// Replaces legacy NotificationsHandler with modern middleware-friendly approach
    /// </summary>
    public static class WebhookValidator
    {
        private const string HmacHeaderName = "X-RISKIFIED-HMAC-SHA256";
        private const string ShopDomainHeaderName = "X-RISKIFIED-SHOP-DOMAIN";

        /// <summary>
        /// Validates HMAC signature and parses webhook payload from HttpRequest
        /// Use in ASP.NET Core controller or middleware
        /// </summary>
        /// <param name="request">The incoming HTTP request</param>
        /// <param name="authToken">Your Riskified authentication token</param>
        /// <param name="expectedShopDomain">Your shop domain (optional validation)</param>
        /// <returns>Parsed OrderNotification if valid</returns>
        /// <exception cref="RiskifiedAuthenticationException">Thrown when HMAC validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown when parsing fails</exception>
        /// <example>
        /// <code>
        /// app.MapPost("/webhooks/riskified", async (HttpRequest request) =>
        /// {
        ///     var notification = await WebhookValidator.ValidateAndParseAsync(
        ///         request,
        ///         authToken: configuration["Riskified:AuthToken"],
        ///         expectedShopDomain: configuration["Riskified:ShopDomain"]
        ///     );
        ///
        ///     // Handle notification
        ///     Console.WriteLine($"Order {notification.Id}: {notification.Status}");
        ///     return Results.Ok();
        /// });
        /// </code>
        /// </example>
        public static async Task<OrderNotification> ValidateAndParseAsync(
            HttpRequest request,
            string authToken,
            string expectedShopDomain = null)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(authToken))
                throw new ArgumentNullException(nameof(authToken));

            // Validate shop domain header if provided
            if (!string.IsNullOrEmpty(expectedShopDomain))
            {
                if (!request.Headers.ContainsKey(ShopDomainHeaderName))
                    throw new RiskifiedAuthenticationException($"Missing {ShopDomainHeaderName} header");

                var shopDomain = request.Headers[ShopDomainHeaderName].ToString();
                if (shopDomain != expectedShopDomain)
                    throw new RiskifiedAuthenticationException($"Shop domain mismatch. Expected: {expectedShopDomain}, Got: {shopDomain}");
            }

            // Read request body
            string body;
            using (var reader = new StreamReader(request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            // Validate HMAC
            if (!request.Headers.ContainsKey(HmacHeaderName))
                throw new RiskifiedAuthenticationException($"Missing {HmacHeaderName} header");

            var providedHmac = request.Headers[HmacHeaderName].ToString();
            var calculatedHmac = CalculateHmac(body, authToken);

            if (!string.Equals(providedHmac, calculatedHmac, StringComparison.Ordinal))
                throw new RiskifiedAuthenticationException("HMAC signature validation failed");

            // Parse notification
            try
            {
                var wrapper = JsonConvert.DeserializeObject<OrderWrapper<Notification>>(body);
                return new OrderNotification(wrapper);
            }
            catch (Exception ex)
            {
                throw new RiskifiedTransactionException("Failed to parse webhook notification", ex);
            }
        }

        /// <summary>
        /// Validates HMAC signature from webhook request
        /// </summary>
        /// <param name="body">The raw request body as string</param>
        /// <param name="providedHmac">The HMAC from X-RISKIFIED-HMAC-SHA256 header</param>
        /// <param name="authToken">Your Riskified authentication token</param>
        /// <returns>True if HMAC is valid, false otherwise</returns>
        public static bool ValidateHmac(string body, string providedHmac, string authToken)
        {
            if (string.IsNullOrEmpty(body))
                return false;
            if (string.IsNullOrEmpty(providedHmac))
                return false;
            if (string.IsNullOrEmpty(authToken))
                return false;

            var calculatedHmac = CalculateHmac(body, authToken);
            return string.Equals(providedHmac, calculatedHmac, StringComparison.Ordinal);
        }

        /// <summary>
        /// Calculates HMAC-SHA256 signature for request body
        /// </summary>
        /// <param name="data">The request body</param>
        /// <param name="authToken">Your Riskified authentication token</param>
        /// <returns>HMAC signature as hex string</returns>
        public static string CalculateHmac(string data, string authToken)
        {
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException("Data cannot be null or empty", nameof(data));
            if (string.IsNullOrEmpty(authToken))
                throw new ArgumentException("Auth token cannot be null or empty", nameof(authToken));

            byte[] key = Encoding.ASCII.GetBytes(authToken);
            using var hmac = new HMACSHA256(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] hashBytes = hmac.ComputeHash(dataBytes);

            var result = new StringBuilder(hashBytes.Length * 2);
            foreach (byte b in hashBytes)
            {
                result.AppendFormat("{0:x2}", b);
            }
            return result.ToString();
        }
    }
}
