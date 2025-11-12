using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Riskified.SDK.Exceptions;
using Riskified.SDK.Model;
using Riskified.SDK.Model.Internal;
using Riskified.SDK.Model.OrderElements;
using Riskified.SDK.Utils;

namespace Riskified.SDK.Clients
{
    /// <summary>
    /// Client for Riskified Order API operations
    /// Handles order lifecycle: creation, submission, updates, cancellations, refunds, fulfillment
    /// </summary>
    public class OrdersClient
    {
        private readonly RiskifiedEnvironment _env;
        private readonly string _authToken;
        private readonly string _shopDomain;
        private readonly Validations _validationMode;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates OrdersClient from IOptions (for dependency injection)
        /// </summary>
        public OrdersClient(
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

        /// <summary>
        /// Creates OrdersClient with explicit parameters
        /// </summary>
        public OrdersClient(
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
        /// Creates a new order without submitting for analysis
        /// </summary>
        public async Task<OrderNotification> CreateAsync(Order order, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(order, HttpUtils.BuildUrl(_env, "/api/create"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Submits an order for fraud analysis
        /// </summary>
        public async Task<OrderNotification> SubmitAsync(Order order, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(order, HttpUtils.BuildUrl(_env, "/api/submit"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing order
        /// </summary>
        public async Task<OrderNotification> UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(order, HttpUtils.BuildUrl(_env, "/api/update"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Requests synchronous fraud decision
        /// </summary>
        public async Task<OrderNotification> DecideAsync(Order order, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(order, HttpUtils.BuildUrl(_env, "/api/decide", FlowStrategy.Sync), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Cancels an order
        /// </summary>
        public async Task<OrderNotification> CancelAsync(OrderCancellation orderCancellation, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderCancellation, HttpUtils.BuildUrl(_env, "/api/cancel"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports partial refund for an order
        /// </summary>
        public async Task<OrderNotification> PartlyRefundAsync(OrderPartialRefund orderPartialRefund, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderPartialRefund, HttpUtils.BuildUrl(_env, "/api/refund"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports order fulfillment/shipment
        /// </summary>
        public async Task<OrderNotification> FulfillAsync(OrderFulfillment orderFulfillment, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderFulfillment, HttpUtils.BuildUrl(_env, "/api/fulfill"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Communicates merchant's decision on an order
        /// </summary>
        public async Task<OrderNotification> DecisionAsync(OrderDecision orderDecision, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderDecision, HttpUtils.BuildUrl(_env, "/api/decision"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports chargeback event
        /// </summary>
        public async Task<OrderNotification> ChargebackAsync(OrderChargeback orderChargeback, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderChargeback, HttpUtils.BuildUrl(_env, "/api/chargeback"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends historical orders in batches
        /// </summary>
        public async Task<(bool Success, Dictionary<string, string> FailedOrders)> SendHistoricalOrdersAsync(
            IEnumerable<Order> orders,
            CancellationToken cancellationToken = default)
        {
            const byte batchSize = 10;

            if (orders == null)
            {
                return (true, null);
            }

            var errors = new Dictionary<string, string>();
            var riskifiedEndpointUrl = HttpUtils.BuildUrl(_env, "/api/historical");
            var batch = new List<Order>(batchSize);
            var enumerator = orders.GetEnumerator();

            do
            {
                batch.Clear();
                while (batch.Count < batchSize && enumerator.MoveNext())
                {
                    var order = enumerator.Current;
                    try
                    {
                        if (_validationMode != Validations.Skip)
                        {
                            order.Validate(_validationMode);
                        }
                        batch.Add(order);
                    }
                    catch (OrderFieldBadFormatException e)
                    {
                        errors.Add(order.Id, e.Message);
                    }
                }

                if (batch.Count > 0)
                {
                    var wrappedOrders = new OrdersWrapper(batch);
                    try
                    {
                        var httpClient = _httpClientFactory.CreateClient("RiskifiedClient");
                        await HttpUtils.JsonPostAndParseResponseToObjectAsync<OrdersWrapper>(
                            riskifiedEndpointUrl, wrappedOrders, _authToken, _shopDomain, httpClient, cancellationToken).ConfigureAwait(false);
                    }
                    catch (RiskifiedTransactionException e)
                    {
                        batch.ForEach(o => errors.Add(o.Id, e.Message));
                    }
                }
            } while (batch.Count == batchSize);

            return errors.Count == 0 ? (true, null) : (false, errors);
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
