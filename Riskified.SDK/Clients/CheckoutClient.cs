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
        /// Processes checkout and creates order record for pre-authorization fraud screening.
        /// Use before payment gateway authorization to get fraud assessment.
        /// </summary>
        /// <param name="orderCheckout">Checkout data including cart and customer information</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification with fraud assessment</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when checkout validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        /// <remarks>
        /// Call before payment authorization to screen transactions.
        /// Helps prevent fraudulent authorizations and reduce costs.
        /// </remarks>
        public async Task<OrderNotification> CheckoutAsync(OrderCheckout orderCheckout, CancellationToken cancellationToken = default)
        {
            return await SendOrderCheckoutAsync(orderCheckout, HttpUtils.BuildUrl(_env, "/api/checkout_create"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Requests proactive pre-checkout fraud screening (Screen product).
        /// Provides fraud assessment before customer enters payment gateway, reducing friction.
        /// </summary>
        /// <param name="orderCheckout">Checkout data for fraud screening</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification with fraud risk assessment</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when checkout validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        /// <remarks>
        /// Screen provides proactive fraud review before checkout.
        /// Also supports PSD2/SCA optimization to reduce authentication friction.
        /// </remarks>
        public async Task<OrderNotification> AdviseAsync(OrderCheckout orderCheckout, CancellationToken cancellationToken = default)
        {
            return await SendOrderCheckoutAsync(orderCheckout, HttpUtils.BuildUrl(_env, "/api/advise"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports denied checkout when payment gateway declines authorization.
        /// Notifies Riskified of authorization failures for analytics and reporting.
        /// </summary>
        /// <param name="orderCheckout">Checkout denial details</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification confirming denial recorded</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
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
