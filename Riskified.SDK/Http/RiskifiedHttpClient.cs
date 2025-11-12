using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Riskified.SDK.Exceptions;

namespace Riskified.SDK.Http
{
    /// <summary>
    /// Modern HttpClient-based HTTP communication layer for Riskified SDK
    /// Replaces legacy WebRequest implementation with async/await support
    /// </summary>
    internal class RiskifiedHttpClient
    {
        private const string ShopDomainHeaderName = "X-RISKIFIED-SHOP-DOMAIN";
        private const string HmacHeaderName = "X-RISKIFIED-HMAC-SHA256";
        private const int ServerApiVersion = 2;

        private readonly HttpClient _httpClient;
        private readonly string _assemblyVersion;
        private readonly ILogger _logger;

        public RiskifiedHttpClient(HttpClient httpClient, ILogger logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? NullLogger.Instance;
            _assemblyVersion = typeof(RiskifiedHttpClient).Assembly.GetName().Version.ToString();
        }

        /// <summary>
        /// Sends an HTTP POST request asynchronously with JSON-serialized data
        /// </summary>
        public async Task<TResponse> PostJsonAsync<TRequest, TResponse>(
            Uri url,
            TRequest requestObj,
            string authToken,
            string shopDomain,
            CancellationToken cancellationToken = default)
            where TRequest : class
            where TResponse : class
        {
            var jsonContent = SerializeToJson(requestObj);
            var request = CreatePostRequest(url, jsonContent, authToken, shopDomain);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                return await HandleResponseAsync<TResponse>(response, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                const string errorMsg = "HTTP request failed";
                _logger.LogError(errorMsg, ex);
                throw new RiskifiedTransactionException(errorMsg, ex);
            }
            catch (TaskCanceledException ex)
            {
                const string errorMsg = "HTTP request timed out";
                _logger.LogError(errorMsg, ex);
                throw new RiskifiedTransactionException(errorMsg, ex);
            }
        }

        /// <summary>
        /// Sends an HTTP POST request asynchronously without expecting a response body
        /// </summary>
        public async Task PostJsonAsync<TRequest>(
            Uri url,
            TRequest requestObj,
            string authToken,
            string shopDomain,
            CancellationToken cancellationToken = default)
            where TRequest : class
        {
            var jsonContent = SerializeToJson(requestObj);
            var request = CreatePostRequest(url, jsonContent, authToken, shopDomain);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await EnsureSuccessStatusCodeAsync(response, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                const string errorMsg = "HTTP request failed";
                _logger.LogError(errorMsg, ex);
                throw new RiskifiedTransactionException(errorMsg, ex);
            }
            catch (TaskCanceledException ex)
            {
                const string errorMsg = "HTTP request timed out";
                _logger.LogError(errorMsg, ex);
                throw new RiskifiedTransactionException(errorMsg, ex);
            }
        }

        private HttpRequestMessage CreatePostRequest(Uri url, string jsonBody, string authToken, string shopDomain)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // Add custom Riskified headers
            AddDefaultHeaders(request.Headers, authToken, shopDomain, jsonBody);

            // Set content
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Set standard headers
            request.Headers.UserAgent.ParseAdd($"Riskified.SDK_NET/{_assemblyVersion}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return request;
        }

        private void AddDefaultHeaders(HttpRequestHeaders headers, string authToken, string shopDomain, string body)
        {
            string hmac = CalculateHmac(body, authToken);
            headers.Add(HmacHeaderName, hmac);
            headers.Add(ShopDomainHeaderName, shopDomain);
            headers.Add("Accept-Encoding", "gzip,deflate,sdch");
            headers.Add("API-VERSION", ServerApiVersion.ToString());
        }

        private string CalculateHmac(string data, string authToken)
        {
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

        private string SerializeToJson<T>(T obj) where T : class
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = new[] { new Newtonsoft.Json.Converters.StringEnumConverter() }
                };
                return JsonConvert.SerializeObject(obj, settings);
            }
            catch (Exception ex)
            {
                throw new OrderFieldBadFormatException($"Failed to serialize object to JSON: {ex.Message}", ex);
            }
        }

        private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) where T : class
        {
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return DeserializeFromJson<T>(content);
            }

            // Handle error response
            await HandleErrorResponseAsync(response, cancellationToken).ConfigureAwait(false);
            return null; // Never reached due to exception
        }

        private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandleErrorResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            string errorMessage = $"HTTP request failed with status code {(int)response.StatusCode} ({response.StatusCode})";

            try
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(content);

                if (errorResponse?.Error?.Message != null)
                {
                    errorMessage = $"{errorResponse.Error.Message} (HTTP {(int)response.StatusCode})";
                }
            }
            catch
            {
                // If we can't parse the error response, use the default message
            }

            _logger.LogError(errorMessage);
            throw new RiskifiedTransactionException(errorMessage);
        }

        private T DeserializeFromJson<T>(string json) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                string errorMsg = $"Failed to deserialize JSON response to type {typeof(T).Name}. Body: {json}";
                throw new RiskifiedTransactionException(errorMsg, ex);
            }
        }

        private class ErrorResponse
        {
            [JsonProperty(PropertyName = "error")]
            public ErrorMessage Error { get; set; }
        }

        private class ErrorMessage
        {
            [JsonProperty(PropertyName = "message")]
            public string Message { get; set; }
        }
    }
}
