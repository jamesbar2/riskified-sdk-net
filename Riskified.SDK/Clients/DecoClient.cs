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
    /// Client for Riskified Deco Payment API operations
    /// Handles Deco payment eligibility and opt-in flow
    /// </summary>
    public class DecoClient
    {
        private readonly RiskifiedEnvironment _env;
        private readonly string _authToken;
        private readonly string _shopDomain;
        private readonly Validations _validationMode;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;

        public DecoClient(
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

        public DecoClient(
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
        /// Checks if order is eligible for Deco payment
        /// </summary>
        public async Task<OrderNotification> EligibleAsync(OrderIdOnly orderIdOnly, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderIdOnly, HttpUtils.BuildUrl(_env, "/api/eligible", FlowStrategy.Deco), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Opts order into Deco payment
        /// </summary>
        public async Task<OrderNotification> OptInAsync(OrderIdOnly orderIdOnly, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderIdOnly, HttpUtils.BuildUrl(_env, "/api/opt_in", FlowStrategy.Deco), cancellationToken).ConfigureAwait(false);
        }

        private async Task<OrderNotification> SendOrderAsync(AbstractOrder order, Uri riskifiedEndpointUrl, CancellationToken cancellationToken = default)
        {
            if (_validationMode != Validations.Skip)
            {
                order.Validate(_validationMode);
            }
            var wrappedOrder = new OrderWrapper<AbstractOrder>(order);
            var httpClient = _httpClientFactory.CreateClient("RiskifiedClient");
            var transactionResult = await HttpUtils.JsonPostAndParseResponseToObjectAsync<OrderWrapper<Notification>, OrderWrapper<AbstractOrder>>(
                riskifiedEndpointUrl, wrappedOrder, _authToken, _shopDomain, httpClient, cancellationToken).ConfigureAwait(false);
            return new OrderNotification(transactionResult);
        }
    }
}
