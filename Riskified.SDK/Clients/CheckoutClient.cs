using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Riskified.SDK.Model;
using Riskified.SDK.Model.Internal;
using Riskified.SDK.Utils;

namespace Riskified.SDK.Clients
{
    /// <summary>
    /// Client for Riskified Checkout API operations
    /// Handles pre-checkout screening and payment optimization
    /// </summary>
    public class CheckoutClient
    {
        private readonly RiskifiedEnvironment _env;
        private readonly string _authToken;
        private readonly string _shopDomain;
        private readonly Validations _validationMode;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;

        public CheckoutClient(
            IOptions<RiskifiedOptions> options,
            IHttpClientFactory httpClientFactory = null)
            : this(
                options?.Value?.Environment ?? throw new ArgumentNullException(nameof(options)),
                options.Value.MerchantAuthenticationToken ?? throw new ArgumentException("MerchantAuthenticationToken is required"),
                options.Value.MerchantDomain ?? throw new ArgumentException("MerchantDomain is required"),
                options.Value.ValidationMode,
                httpClientFactory)
        {
        }

        public CheckoutClient(
            RiskifiedEnvironment env,
            string authToken,
            string shopDomain,
            Validations validationMode = Validations.All,
            IHttpClientFactory httpClientFactory = null)
        {
            _env = env;
            _authToken = authToken;
            _shopDomain = shopDomain;
            _validationMode = validationMode;

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
        /// Processes checkout and creates order record
        /// </summary>
        public async Task<OrderNotification> CheckoutAsync(OrderCheckout orderCheckout, CancellationToken cancellationToken = default)
        {
            return await SendOrderCheckoutAsync(orderCheckout, HttpUtils.BuildUrl(_env, "/api/checkout_create"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Requests pre-checkout fraud screening (Screen/PSD2)
        /// </summary>
        public async Task<OrderNotification> AdviseAsync(OrderCheckout orderCheckout, CancellationToken cancellationToken = default)
        {
            return await SendOrderCheckoutAsync(orderCheckout, HttpUtils.BuildUrl(_env, "/api/advise"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports denied checkout (authorization failure)
        /// </summary>
        public async Task<OrderNotification> CheckoutDeniedAsync(OrderCheckoutDenied orderCheckout, CancellationToken cancellationToken = default)
        {
            return await SendOrderCheckoutAsync(orderCheckout, HttpUtils.BuildUrl(_env, "/api/checkout_denied"), cancellationToken).ConfigureAwait(false);
        }

        private async Task<OrderNotification> SendOrderCheckoutAsync(AbstractOrder orderCheckout, Uri riskifiedEndpointUrl, CancellationToken cancellationToken = default)
        {
            if (_validationMode != Validations.Skip)
            {
                orderCheckout.Validate(_validationMode);
            }
            var wrappedOrder = new OrderCheckoutWrapper<AbstractOrder>(orderCheckout);
            var httpClient = _httpClientFactory.CreateClient("RiskifiedClient");
            var transactionResult = await HttpUtils.JsonPostAndParseResponseToObjectAsync<OrderCheckoutWrapper<Notification>, OrderCheckoutWrapper<AbstractOrder>>(
                riskifiedEndpointUrl, wrappedOrder, _authToken, _shopDomain, httpClient, cancellationToken).ConfigureAwait(false);
            return new OrderNotification(transactionResult);
        }
    }
}
