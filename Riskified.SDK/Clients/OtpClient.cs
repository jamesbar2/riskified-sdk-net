using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Riskified.SDK.Model.OrderElements;
using Riskified.SDK.Model.OtpElements;
using Riskified.SDK.Utils;

namespace Riskified.SDK.Clients
{
    /// <summary>
    /// Client for Riskified OTP Recovery API operations
    /// Handles one-time password initiation for account recovery
    /// </summary>
    public class OtpClient
    {
        private readonly RiskifiedEnvironment _env;
        private readonly string _authToken;
        private readonly string _shopDomain;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;

        public OtpClient(
            IOptions<RiskifiedOptions> options,
            IHttpClientFactory httpClientFactory = null)
            : this(
                options?.Value?.Environment ?? throw new ArgumentNullException(nameof(options)),
                options.Value.MerchantAuthenticationToken ?? throw new ArgumentException("MerchantAuthenticationToken is required"),
                options.Value.MerchantDomain ?? throw new ArgumentException("MerchantDomain is required"),
                httpClientFactory)
        {
        }

        public OtpClient(
            RiskifiedEnvironment env,
            string authToken,
            string shopDomain,
            IHttpClientFactory httpClientFactory = null)
        {
            _env = env;
            _authToken = authToken;
            _shopDomain = shopDomain;

            if (httpClientFactory == null)
            {
                var services = new ServiceCollection();
                services.AddRiskifiedHttpClient();
                _serviceProvider = services.BuildServiceProvider();
                _httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            }
            else
            {
                _httpClientFactory = httpClientFactory;
                _serviceProvider = null;
            }
        }

        /// <summary>
        /// Initiates OTP for account recovery
        /// </summary>
        public async Task<OtpWidgetNotification> InitiateOtpAsync(OtpInitiate otpInitiate, CancellationToken cancellationToken = default)
        {
            var httpClient = _httpClientFactory.CreateClient("RiskifiedClient");
            return await HttpUtils.JsonPostAndParseResponseToObjectAsync<OtpWidgetNotification, OtpInitiate>(
                HttpUtils.BuildUrl(_env, "/recover/v1/otp/initiate", FlowStrategy.Otp),
                otpInitiate,
                _authToken,
                _shopDomain,
                httpClient,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
