using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Riskified.SDK.Model.AccountActionElements;
using Riskified.SDK.Utils;

namespace Riskified.SDK.Clients
{
    /// <summary>
    /// Client for Riskified Account Secure API operations
    /// Handles customer authentication, account management, and security events
    /// </summary>
    public class AccountClient
    {
        private readonly RiskifiedEnvironment _env;
        private readonly string _authToken;
        private readonly string _shopDomain;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;

        public AccountClient(
            IOptions<RiskifiedOptions> options,
            IHttpClientFactory httpClientFactory = null)
            : this(
                options?.Value?.Environment ?? throw new ArgumentNullException(nameof(options)),
                options.Value.MerchantAuthenticationToken ?? throw new ArgumentException("MerchantAuthenticationToken is required"),
                options.Value.MerchantDomain ?? throw new ArgumentException("MerchantDomain is required"),
                httpClientFactory)
        {
        }

        public AccountClient(
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
        /// Analyzes customer login attempt
        /// </summary>
        public async Task<AccountActionNotification> LoginAsync(Login login, CancellationToken cancellationToken = default)
        {
            return await SendAccountActionAsync(login, HttpUtils.BuildUrl(_env, "/customers/login", FlowStrategy.Account), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports customer logout
        /// </summary>
        public async Task<AccountActionNotification> LogoutAsync(Logout logout, CancellationToken cancellationToken = default)
        {
            return await SendAccountActionAsync(logout, HttpUtils.BuildUrl(_env, "/customers/logout", FlowStrategy.Account), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports new customer account creation
        /// </summary>
        public async Task<AccountActionNotification> CustomerCreateAsync(CustomerCreate customerCreate, CancellationToken cancellationToken = default)
        {
            return await SendAccountActionAsync(customerCreate, HttpUtils.BuildUrl(_env, "/customers/customer_create", FlowStrategy.Account), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports customer account updates
        /// </summary>
        public async Task<AccountActionNotification> CustomerUpdateAsync(CustomerUpdate customerUpdate, CancellationToken cancellationToken = default)
        {
            return await SendAccountActionAsync(customerUpdate, HttpUtils.BuildUrl(_env, "/customers/customer_update", FlowStrategy.Account), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports password reset request
        /// </summary>
        public async Task<AccountActionNotification> ResetPasswordRequestAsync(ResetPasswordRequest resetPasswordRequest, CancellationToken cancellationToken = default)
        {
            return await SendAccountActionAsync(resetPasswordRequest, HttpUtils.BuildUrl(_env, "/customers/reset_password", FlowStrategy.Account), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports wishlist changes
        /// </summary>
        public async Task<AccountActionNotification> WishlistChangesAsync(WishlistChanges wishlistChanges, CancellationToken cancellationToken = default)
        {
            return await SendAccountActionAsync(wishlistChanges, HttpUtils.BuildUrl(_env, "/customers/wishlist", FlowStrategy.Account), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports reward/coupon redemption
        /// </summary>
        public async Task<AccountActionNotification> RedeemAsync(Redeem redeem, CancellationToken cancellationToken = default)
        {
            return await SendAccountActionAsync(redeem, HttpUtils.BuildUrl(_env, "/customers/redeem", FlowStrategy.Account), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports customer service interaction
        /// </summary>
        public async Task<AccountActionNotification> CustomerReachOutAsync(CustomerReachOut customerReachOut, CancellationToken cancellationToken = default)
        {
            return await SendAccountActionAsync(customerReachOut, HttpUtils.BuildUrl(_env, "/customers/contact", FlowStrategy.Account), cancellationToken).ConfigureAwait(false);
        }

        private async Task<AccountActionNotification> SendAccountActionAsync(AbstractAccountAction accountAction, Uri riskifiedEndpointUrl, CancellationToken cancellationToken = default)
        {
            var httpClient = _httpClientFactory.CreateClient("RiskifiedClient");
            return await HttpUtils.JsonPostAndParseResponseToObjectAsync<AccountActionNotification, AbstractAccountAction>(
                riskifiedEndpointUrl, accountAction, _authToken, _shopDomain, httpClient, cancellationToken).ConfigureAwait(false);
        }
    }
}
