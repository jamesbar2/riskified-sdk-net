using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Riskified.SDK.Clients;
using Riskified.SDK.Model;
using Riskified.SDK.Model.AccountActionElements;
using Riskified.SDK.Model.OrderElements;
using Riskified.SDK.Model.OtpElements;
using Riskified.SDK.Utils;

namespace Riskified.SDK.Orders
{
    /// <summary>
    /// Legacy gateway class for backward compatibility.
    /// Use specialized clients instead: OrdersClient, CheckoutClient, AccountClient, DecoClient, OtpClient
    /// </summary>
    [Obsolete("Use OrdersClient, CheckoutClient, AccountClient, DecoClient, or OtpClient instead for better separation of concerns. OrdersGateway will be removed in v6.0.")]
    public class OrdersGateway
    {
        private readonly OrdersClient _ordersClient;
        private readonly CheckoutClient _checkoutClient;
        private readonly AccountClient _accountClient;
        private readonly DecoClient _decoClient;
        private readonly OtpClient _otpClient;

        public OrdersGateway(IOptions<RiskifiedOptions> options, IHttpClientFactory httpClientFactory = null)
        {
            _ordersClient = new OrdersClient(options, httpClientFactory);
            _checkoutClient = new CheckoutClient(options, httpClientFactory);
            _accountClient = new AccountClient(options, httpClientFactory);
            _decoClient = new DecoClient(options, httpClientFactory);
            _otpClient = new OtpClient(options, httpClientFactory);
        }

        public OrdersGateway(RiskifiedEnvironment env, string authToken, string shopDomain)
            : this(env, authToken, shopDomain, Validations.All)
        {
        }

        public OrdersGateway(RiskifiedEnvironment env, string authToken, string shopDomain, bool shouldUseWeakValidation)
            : this(env, authToken, shopDomain, shouldUseWeakValidation ? Validations.Weak : Validations.All)
        {
        }

        public OrdersGateway(RiskifiedEnvironment env, string authToken, string shopDomain, Validations validationMode)
        {
            _ordersClient = new OrdersClient(env, authToken, shopDomain, validationMode);
            _checkoutClient = new CheckoutClient(env, authToken, shopDomain, validationMode);
            _accountClient = new AccountClient(env, authToken, shopDomain);
            _decoClient = new DecoClient(env, authToken, shopDomain, validationMode);
            _otpClient = new OtpClient(env, authToken, shopDomain);
        }

        // Checkout operations - delegate to CheckoutClient
        public OrderNotification Checkout(OrderCheckout orderCheckout)
            => _checkoutClient.CheckoutAsync(orderCheckout).GetAwaiter().GetResult();

        public OrderNotification Advise(OrderCheckout orderCheckout)
            => _checkoutClient.AdviseAsync(orderCheckout).GetAwaiter().GetResult();

        public OrderNotification CheckoutDenied(OrderCheckoutDenied orderCheckout)
            => _checkoutClient.CheckoutDeniedAsync(orderCheckout).GetAwaiter().GetResult();

        // Order operations - delegate to OrdersClient
        public OrderNotification Create(Order order)
            => _ordersClient.CreateAsync(order).GetAwaiter().GetResult();

        public OrderNotification Update(Order order)
            => _ordersClient.UpdateAsync(order).GetAwaiter().GetResult();

        public OrderNotification Submit(Order order)
            => _ordersClient.SubmitAsync(order).GetAwaiter().GetResult();

        public OrderNotification Decide(Order order)
            => _ordersClient.DecideAsync(order).GetAwaiter().GetResult();

        public OrderNotification Cancel(OrderCancellation orderCancellation)
            => _ordersClient.CancelAsync(orderCancellation).GetAwaiter().GetResult();

        public OrderNotification PartlyRefund(OrderPartialRefund orderPartialRefund)
            => _ordersClient.PartlyRefundAsync(orderPartialRefund).GetAwaiter().GetResult();

        public OrderNotification Fulfill(OrderFulfillment orderFulfillment)
            => _ordersClient.FulfillAsync(orderFulfillment).GetAwaiter().GetResult();

        public OrderNotification Decision(OrderDecision orderDecision)
            => _ordersClient.DecisionAsync(orderDecision).GetAwaiter().GetResult();

        public OrderNotification Chargeback(OrderChargeback orderChargeback)
            => _ordersClient.ChargebackAsync(orderChargeback).GetAwaiter().GetResult();

        public bool SendHistoricalOrders(IEnumerable<Order> orders, out Dictionary<string, string> failedOrders)
        {
            var (success, failed) = _ordersClient.SendHistoricalOrdersAsync(orders).GetAwaiter().GetResult();
            failedOrders = failed;
            return success;
        }

        // Account operations - delegate to AccountClient
        public AccountActionNotification Login(Login login)
            => _accountClient.LoginAsync(login).GetAwaiter().GetResult();

        public AccountActionNotification CustomerCreate(CustomerCreate customerCreate)
            => _accountClient.CustomerCreateAsync(customerCreate).GetAwaiter().GetResult();

        public AccountActionNotification CustomerUpdate(CustomerUpdate customerUpdate)
            => _accountClient.CustomerUpdateAsync(customerUpdate).GetAwaiter().GetResult();

        public AccountActionNotification Logout(Logout logout)
            => _accountClient.LogoutAsync(logout).GetAwaiter().GetResult();

        public AccountActionNotification ResetPasswordRequest(ResetPasswordRequest resetPasswordRequest)
            => _accountClient.ResetPasswordRequestAsync(resetPasswordRequest).GetAwaiter().GetResult();

        public AccountActionNotification WishlistChanges(WishlistChanges wishlistChanges)
            => _accountClient.WishlistChangesAsync(wishlistChanges).GetAwaiter().GetResult();

        public AccountActionNotification Redeem(Redeem redeem)
            => _accountClient.RedeemAsync(redeem).GetAwaiter().GetResult();

        public AccountActionNotification CustomerReachOut(CustomerReachOut customerReachOut)
            => _accountClient.CustomerReachOutAsync(customerReachOut).GetAwaiter().GetResult();

        // Deco operations - delegate to DecoClient
        public OrderNotification Eligible(OrderIdOnly orderIdOnly)
            => _decoClient.EligibleAsync(orderIdOnly).GetAwaiter().GetResult();

        public OrderNotification OptIn(OrderIdOnly orderIdOnly)
            => _decoClient.OptInAsync(orderIdOnly).GetAwaiter().GetResult();

        // OTP operations - delegate to OtpClient
        public OtpWidgetNotification InitiateOtp(OtpInitiate otpInitiate)
            => _otpClient.InitiateOtpAsync(otpInitiate).GetAwaiter().GetResult();
    }
}
