using Microsoft.Extensions.Configuration;
using Riskified.SDK.Clients;
using Riskified.SDK.Orders;
using Riskified.SDK.Utils;

namespace Riskified.SDK.Tests;

/// <summary>
/// Test fixture for Riskified SDK tests
/// Follows xUnit IAsyncLifetime pattern for async setup/teardown
/// </summary>
public sealed class RiskifiedTestFixture : IAsyncLifetime
{
    public IConfiguration Configuration { get; private set; }
    public OrdersGateway Gateway { get; private set; }

    // New specialized clients
    public OrdersClient OrdersClient { get; private set; }
    public CheckoutClient CheckoutClient { get; private set; }
    public AccountClient AccountClient { get; private set; }

    public string MerchantDomain { get; private set; }
    public string AuthToken { get; private set; }
    public RiskifiedEnvironment Environment { get; private set; }

    public RiskifiedTestFixture()
    {
        // Build configuration with multiple sources (priority: User Secrets > Env Vars > Local JSON > Base JSON)
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testsettings.json", optional: false)
            .AddJsonFile("testsettings.Local.json", optional: true) // For local JSON credentials (git-ignored)
            .AddEnvironmentVariables() // Allow environment variable overrides
            .AddUserSecrets<RiskifiedTestFixture>() // Secure local secrets (recommended)
            .Build();

        // Load Riskified configuration
        MerchantDomain = Configuration["Riskified:MerchantDomain"]
            ?? throw new InvalidOperationException("Missing Riskified:MerchantDomain in test configuration");

        AuthToken = Configuration["Riskified:MerchantAuthenticationToken"]
            ?? throw new InvalidOperationException("Missing Riskified:MerchantAuthenticationToken in test configuration");

        var envString = Configuration["Riskified:RiskifiedEnvironment"] ?? "Sandbox";
        Environment = Enum.Parse<RiskifiedEnvironment>(envString);

        // Initialize legacy OrdersGateway for backward compat tests
        Gateway = new OrdersGateway(Environment, AuthToken, MerchantDomain);

        // Initialize modern specialized clients
        OrdersClient = new OrdersClient(Environment, AuthToken, MerchantDomain);
        CheckoutClient = new CheckoutClient(Environment, AuthToken, MerchantDomain);
        AccountClient = new AccountClient(Environment, AuthToken, MerchantDomain);
    }

    public Task InitializeAsync()
    {
        // Async initialization if needed (e.g., create test data in sandbox)
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        // Async cleanup if needed
        return Task.CompletedTask;
    }
}
