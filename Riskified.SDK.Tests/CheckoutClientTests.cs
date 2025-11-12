using Riskified.SDK.Clients;
using Riskified.SDK.Model;
using Riskified.SDK.Model.OrderElements;

namespace Riskified.SDK.Tests;

/// <summary>
/// Tests for CheckoutClient
/// Tests pre-checkout screening and payment optimization
/// </summary>
public class CheckoutClientTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public CheckoutClientTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void CheckoutClient_Initializes()
    {
        Assert.NotNull(_fixture.CheckoutClient);
    }


    [Fact]
    public void CheckoutClient_OnlyHasAsyncMethods()
    {
        var methods = typeof(CheckoutClient).GetMethods()
            .Where(m => m.IsPublic && m.DeclaringType == typeof(CheckoutClient) && !m.IsSpecialName)
            .ToArray();

        foreach (var method in methods)
        {
            Assert.EndsWith("Async", method.Name);
        }
    }
}
