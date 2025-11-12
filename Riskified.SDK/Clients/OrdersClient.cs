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
        /// Creates a new order record without submitting for fraud analysis.
        /// Use this for post-authorization order creation when you want to track the order but analyze it later.
        /// </summary>
        /// <param name="order">The order to create with complete transaction details</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification containing status and order ID</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when order validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        /// <remarks>
        /// Use Create when you want to register an order without immediate fraud analysis.
        /// Follow with Submit when ready for analysis.
        /// See: https://apiref.riskified.com
        /// </remarks>
        public async Task<OrderNotification> CreateAsync(Order order, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(order, HttpUtils.BuildUrl(_env, "/api/create"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Submits an order for fraud analysis. This is the primary method for fraud prevention.
        /// Riskified will analyze the order and send decision via webhook (async) or return immediately (sync plans).
        /// </summary>
        /// <param name="order">The order to submit for fraud analysis</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification with initial status (final decision via webhook)</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when order validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        /// <remarks>
        /// This is the main fraud prevention endpoint. Use for all orders requiring fraud analysis.
        /// Async plans: Decision sent via webhook. Sync plans: Decision in response.
        /// </remarks>
        public async Task<OrderNotification> SubmitAsync(Order order, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(order, HttpUtils.BuildUrl(_env, "/api/submit"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing order with new information.
        /// Use when order details change after initial creation/submission.
        /// </summary>
        /// <param name="order">The order with updated information (must include order ID)</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification confirming the update</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when order validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        public async Task<OrderNotification> UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(order, HttpUtils.BuildUrl(_env, "/api/update"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Requests synchronous fraud decision (available on sync plans only).
        /// Blocks and returns immediate fraud decision without webhook callback.
        /// </summary>
        /// <param name="order">The order to analyze</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification with immediate fraud decision</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when order validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        /// <remarks>
        /// Only available for merchants with synchronous review plans.
        /// Returns immediate approve/decline decision in the response.
        /// </remarks>
        public async Task<OrderNotification> DecideAsync(Order order, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(order, HttpUtils.BuildUrl(_env, "/api/decide", FlowStrategy.Sync), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Notifies Riskified of order cancellation for charge fee adjustments.
        /// Call this when an order is fully cancelled or refunded.
        /// </summary>
        /// <param name="orderCancellation">Cancellation details including order ID, timestamp, and reason</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification confirming cancellation recorded</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        public async Task<OrderNotification> CancelAsync(OrderCancellation orderCancellation, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderCancellation, HttpUtils.BuildUrl(_env, "/api/cancel"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports partial refund for charge fee adjustments.
        /// Use for partial refunds where order remains partially valid.
        /// </summary>
        /// <param name="orderPartialRefund">Partial refund details including refunded items and amounts</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification confirming refund recorded</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        public async Task<OrderNotification> PartlyRefundAsync(OrderPartialRefund orderPartialRefund, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderPartialRefund, HttpUtils.BuildUrl(_env, "/api/refund"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports order fulfillment with shipping/tracking information.
        /// Call when order ships to update Riskified with fulfillment status.
        /// </summary>
        /// <param name="orderFulfillment">Fulfillment details including tracking numbers and shipping info</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification confirming fulfillment recorded</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        public async Task<OrderNotification> FulfillAsync(OrderFulfillment orderFulfillment, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderFulfillment, HttpUtils.BuildUrl(_env, "/api/fulfill"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Communicates merchant's external decision on an order.
        /// Updates Riskified with your decision (approved/declined) for analytics and reporting.
        /// </summary>
        /// <param name="orderDecision">Decision details with order ID and external status</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification confirming decision recorded</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        public async Task<OrderNotification> DecisionAsync(OrderDecision orderDecision, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderDecision, HttpUtils.BuildUrl(_env, "/api/decision"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports chargeback submission (contact Riskified support before first use).
        /// Use to notify Riskified when a chargeback is filed against an order.
        /// </summary>
        /// <param name="orderChargeback">Chargeback details including dispute information</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Order notification confirming chargeback recorded</returns>
        /// <exception cref="OrderFieldBadFormatException">Thrown when validation fails</exception>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        /// <remarks>
        /// Contact Riskified support before using this endpoint for the first time.
        /// </remarks>
        public async Task<OrderNotification> ChargebackAsync(OrderChargeback orderChargeback, CancellationToken cancellationToken = default)
        {
            return await SendOrderAsync(orderChargeback, HttpUtils.BuildUrl(_env, "/api/chargeback"), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends historical orders in batches for analysis.
        /// Use during onboarding to submit past orders for baseline fraud analysis.
        /// </summary>
        /// <param name="orders">Collection of historical orders (sent in batches of 10)</param>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>Tuple with success status and dictionary of failed orders (if any)</returns>
        /// <exception cref="RiskifiedTransactionException">Thrown on network or server errors</exception>
        /// <remarks>
        /// Orders are automatically batched (10 per request).
        /// FinancialStatus field must contain the latest order status.
        /// See: https://apiref.riskified.com for historical order requirements.
        /// </remarks>
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
