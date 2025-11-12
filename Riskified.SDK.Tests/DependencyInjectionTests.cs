using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Riskified.SDK.Orders;
using Riskified.SDK.Utils;

namespace Riskified.SDK.Tests;

/// <summary>
/// Tests for dependency injection and IOptions pattern
/// Validates Phase 4 implementation
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void AddRiskified_WithConfiguration_RegistersServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Riskified:MerchantDomain"] = "test.myshopify.com",
                ["Riskified:MerchantAuthenticationToken"] = "test-token",
                ["Riskified:Environment"] = "Sandbox"
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddRiskified(configuration.GetSection("Riskified"));
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var gateway = serviceProvider.GetService<OrdersGateway>();
        Assert.NotNull(gateway);
    }

    [Fact]
    public void AddRiskified_WithAction_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRiskified(options =>
        {
            options.MerchantDomain = "test.myshopify.com";
            options.MerchantAuthenticationToken = "test-token";
            options.Environment = RiskifiedEnvironment.Sandbox;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var gateway = serviceProvider.GetService<OrdersGateway>();
        Assert.NotNull(gateway);
    }

    [Fact]
    public void OrdersGateway_CanBeResolvedFromDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRiskified(options =>
        {
            options.MerchantDomain = "test.myshopify.com";
            options.MerchantAuthenticationToken = "test-token";
            options.Environment = RiskifiedEnvironment.Sandbox;
            options.ValidationMode = Validations.Weak;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var gateway = serviceProvider.GetRequiredService<OrdersGateway>();

        // Assert
        Assert.NotNull(gateway);
    }

    [Fact]
    public void OrdersGateway_IsRegisteredAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRiskified(options =>
        {
            options.MerchantDomain = "test.myshopify.com";
            options.MerchantAuthenticationToken = "test-token";
        });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var gateway1 = serviceProvider.GetService<OrdersGateway>();
        var gateway2 = serviceProvider.GetService<OrdersGateway>();

        // Assert - Same instance (Singleton)
        Assert.Same(gateway1, gateway2);
    }

    [Fact]
    public void RiskifiedOptions_LoadsFromConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Riskified:MerchantDomain"] = "shop.myshopify.com",
                ["Riskified:MerchantAuthenticationToken"] = "auth-token-123",
                ["Riskified:Environment"] = "Production",
                ["Riskified:ValidationMode"] = "Weak",
                ["Riskified:TimeoutSeconds"] = "60",
                ["Riskified:MaxConnectionsPerServer"] = "20"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<RiskifiedOptions>(configuration.GetSection("Riskified"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<RiskifiedOptions>>();

        // Assert
        Assert.Equal("shop.myshopify.com", options.Value.MerchantDomain);
        Assert.Equal("auth-token-123", options.Value.MerchantAuthenticationToken);
        Assert.Equal(RiskifiedEnvironment.Production, options.Value.Environment);
        Assert.Equal(Validations.Weak, options.Value.ValidationMode);
        Assert.Equal(60, options.Value.TimeoutSeconds);
        Assert.Equal(20, options.Value.MaxConnectionsPerServer);
    }

    [Fact]
    public void RiskifiedOptions_HasDefaultValues()
    {
        // Arrange & Act
        var options = new RiskifiedOptions();

        // Assert
        Assert.Equal(RiskifiedEnvironment.Sandbox, options.Environment);
        Assert.Equal(Validations.All, options.ValidationMode);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.Equal(10, options.MaxConnectionsPerServer);
    }

    [Fact]
    public void RiskifiedOptions_SectionKey_IsCorrect()
    {
        // Assert
        Assert.Equal("Riskified", RiskifiedOptions.SectionKey);
    }

    [Fact]
    public void AddRiskifiedHttpClient_RegistersNamedHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRiskifiedHttpClient();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        var httpClient = httpClientFactory?.CreateClient("RiskifiedClient");

        // Assert
        Assert.NotNull(httpClientFactory);
        Assert.NotNull(httpClient);
        Assert.Equal(TimeSpan.FromSeconds(30), httpClient.Timeout);
    }
}
