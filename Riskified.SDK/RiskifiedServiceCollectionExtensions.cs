using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Riskified.SDK.Clients;
using Riskified.SDK.Orders;

namespace Riskified.SDK
{
    /// <summary>
    /// Extension methods for configuring Riskified SDK services in an IServiceCollection
    /// </summary>
    public static class RiskifiedServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Riskified SDK services with configuration from IConfiguration
        /// Recommended for ASP.NET Core and modern .NET applications
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration section containing Riskified settings</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddRiskified(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return services
                .Configure<RiskifiedOptions>(configuration)
                .AddRiskifiedHttpClient()
                .AddRiskifiedServices();
        }

        /// <summary>
        /// Adds Riskified SDK services with explicit options configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Action to configure RiskifiedOptions</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddRiskified(
            this IServiceCollection services,
            Action<RiskifiedOptions> configureOptions)
        {
            return services
                .Configure(configureOptions)
                .AddRiskifiedHttpClient()
                .AddRiskifiedServices();
        }

        /// <summary>
        /// Adds Riskified HttpClient with recommended configuration
        /// </summary>
        public static IServiceCollection AddRiskifiedHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient("RiskifiedClient", (serviceProvider, client) =>
            {
                // Configure timeout from options if available
                var options = serviceProvider.GetService<IOptions<RiskifiedOptions>>();
                var timeout = options?.Value?.TimeoutSeconds ?? 30;
                client.Timeout = TimeSpan.FromSeconds(timeout);
            })
            .ConfigurePrimaryHttpMessageHandler((serviceProvider) =>
            {
                var options = serviceProvider.GetService<IOptions<RiskifiedOptions>>();
                var maxConnections = options?.Value?.MaxConnectionsPerServer ?? 10;

                return new HttpClientHandler
                {
                    // Enable automatic decompression (gzip, deflate)
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

                    // Connection settings for high throughput
                    MaxConnectionsPerServer = maxConnections,

                    // SSL/TLS settings (Tls12 minimum for netstandard2.0 compatibility)
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                };
            });

            return services;
        }

        /// <summary>
        /// Adds Riskified SDK client services
        /// </summary>
        private static IServiceCollection AddRiskifiedServices(this IServiceCollection services)
        {
            // Register modern specialized clients (recommended)
            services.AddSingleton<OrdersClient>();
            services.AddSingleton<CheckoutClient>();
            services.AddSingleton<AccountClient>();
            services.AddSingleton<DecoClient>();
            services.AddSingleton<OtpClient>();

            // Register legacy OrdersGateway (backward compatibility)
            services.AddSingleton<OrdersGateway>();

            return services;
        }
    }
}
