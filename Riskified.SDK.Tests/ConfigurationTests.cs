using Riskified.SDK.Utils;

namespace Riskified.SDK.Tests;

/// <summary>
/// Tests for configuration loading and parsing
/// Validates that the modernized configuration system works correctly
/// </summary>
public class ConfigurationTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public ConfigurationTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Configuration_Loads_Successfully()
    {
        // Assert
        Assert.NotNull(_fixture.Configuration);
    }

    [Fact]
    public void MerchantDomain_IsConfigured()
    {
        // Assert
        Assert.NotNull(_fixture.MerchantDomain);
        Assert.NotEmpty(_fixture.MerchantDomain);
    }

    [Fact]
    public void AuthToken_IsConfigured()
    {
        // Assert
        Assert.NotNull(_fixture.AuthToken);
        Assert.NotEmpty(_fixture.AuthToken);
    }

    [Fact]
    public void Environment_ParsesCorrectly()
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(RiskifiedEnvironment), _fixture.Environment));
    }

    [Fact]
    public void OrdersGateway_Initializes_Successfully()
    {
        // Assert
        Assert.NotNull(_fixture.Gateway);
    }

    [Fact]
    public void RiskifiedEnvironment_AllValuesAreDefined()
    {
        // Arrange & Act
        var values = Enum.GetValues<RiskifiedEnvironment>();

        // Assert
        Assert.Contains(RiskifiedEnvironment.Debug, values);
        Assert.Contains(RiskifiedEnvironment.Sandbox, values);
        Assert.Contains(RiskifiedEnvironment.Production, values);
    }
}
