using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Riskified.SDK
{
    /// <summary>
    /// Extension methods for configuring Riskified SDK services in an IServiceCollection
    /// </summary>
    public static class RiskifiedServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Riskified HttpClient with recommended configuration
        /// </summary>
        public static IServiceCollection AddRiskifiedHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient("RiskifiedClient", client =>
            {
                // Set reasonable defaults
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    // Enable automatic decompression (gzip, deflate)
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

                    // Connection settings for high throughput
                    MaxConnectionsPerServer = 10,

                    // SSL/TLS settings (Tls12 minimum for netstandard2.0 compatibility)
                    SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                };
            });

            return services;
        }
    }
}
