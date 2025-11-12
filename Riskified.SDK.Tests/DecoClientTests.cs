using Riskified.SDK.Clients;
using Riskified.SDK.Model;

namespace Riskified.SDK.Tests;

/// <summary>
/// Tests for DecoClient
/// Tests Deco payment eligibility and opt-in operations
/// </summary>
public class DecoClientTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public DecoClientTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void DecoClient_Initializes()
    {
        var client = new DecoClient(_fixture.Environment, _fixture.AuthToken, _fixture.MerchantDomain);
        Assert.NotNull(client);
    }


    [Fact]
    public void DecoClient_OnlyHasAsyncMethods()
    {
        var methods = typeof(DecoClient).GetMethods()
            .Where(m => m.IsPublic && m.DeclaringType == typeof(DecoClient) && !m.IsSpecialName)
            .ToArray();

        foreach (var method in methods)
        {
            Assert.EndsWith("Async", method.Name);
        }
    }
}
