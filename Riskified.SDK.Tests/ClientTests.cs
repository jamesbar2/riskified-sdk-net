using Riskified.SDK.Clients;
using Riskified.SDK.Model;
using Riskified.SDK.Model.OrderElements;
using Riskified.SDK.Utils;

namespace Riskified.SDK.Tests;

/// <summary>
/// Tests for specialized client classes (Phase 5)
/// Validates clean separation of concerns with focused async APIs
/// </summary>
public class ClientTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public ClientTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void OrdersClient_CanBeCreated()
    {
        var client = new OrdersClient(RiskifiedEnvironment.Sandbox, "token", "domain");
        Assert.NotNull(client);
    }

    [Fact]
    public void CheckoutClient_CanBeCreated()
    {
        var client = new CheckoutClient(RiskifiedEnvironment.Sandbox, "token", "domain");
        Assert.NotNull(client);
    }

    [Fact]
    public void AccountClient_CanBeCreated()
    {
        var client = new AccountClient(RiskifiedEnvironment.Sandbox, "token", "domain");
        Assert.NotNull(client);
    }

    [Fact]
    public void DecoClient_CanBeCreated()
    {
        var client = new DecoClient(RiskifiedEnvironment.Sandbox, "token", "domain");
        Assert.NotNull(client);
    }

    [Fact]
    public void OtpClient_CanBeCreated()
    {
        var client = new OtpClient(RiskifiedEnvironment.Sandbox, "token", "domain");
        Assert.NotNull(client);
    }

    [Fact]
    public void OrdersGateway_IsObsolete()
    {
        // OrdersGateway should be marked with ObsoleteAttribute
        var obsoleteAttr = typeof(Orders.OrdersGateway).GetCustomAttributes(typeof(ObsoleteAttribute), false);
        Assert.NotEmpty(obsoleteAttr);

        var attr = (ObsoleteAttribute)obsoleteAttr[0];
        Assert.Contains("OrdersClient", attr.Message);
        Assert.Contains("CheckoutClient", attr.Message);
        Assert.Contains("AccountClient", attr.Message);
    }

    [Fact]
    public void OrdersGateway_OnlyHasSyncMethods()
    {
        // Verify OrdersGateway has NO async methods (only sync for backward compat)
        var methods = typeof(Orders.OrdersGateway).GetMethods();

        var asyncMethods = methods.Where(m =>
            m.Name.EndsWith("Async") &&
            m.DeclaringType == typeof(Orders.OrdersGateway)).ToArray();

        Assert.Empty(asyncMethods); // Should have zero async methods
    }

    [Fact]
    public void AllClients_HaveAsyncMethodsOnly()
    {
        // Verify specialized clients only have async methods (no sync-over-async)
        var clientTypes = new[]
        {
            typeof(OrdersClient),
            typeof(CheckoutClient),
            typeof(AccountClient),
            typeof(DecoClient),
            typeof(OtpClient)
        };

        foreach (var clientType in clientTypes)
        {
            var publicMethods = clientType.GetMethods()
                .Where(m => m.IsPublic && m.DeclaringType == clientType && !m.IsSpecialName)
                .ToArray();

            // All public methods should end with "Async"
            foreach (var method in publicMethods)
            {
                Assert.EndsWith("Async", method.Name);
            }
        }
    }
}
