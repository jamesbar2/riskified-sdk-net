using Riskified.SDK.Clients;
using Riskified.SDK.Model.AccountActionElements;
using Riskified.SDK.Model.OrderElements;

namespace Riskified.SDK.Tests;

/// <summary>
/// Tests for AccountClient
/// Tests account security and customer action operations
/// </summary>
public class AccountClientTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public AccountClientTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void AccountClient_Initializes()
    {
        Assert.NotNull(_fixture.AccountClient);
    }


    [Fact]
    public void AccountClient_OnlyHasAsyncMethods()
    {
        var methods = typeof(AccountClient).GetMethods()
            .Where(m => m.IsPublic && m.DeclaringType == typeof(AccountClient) && !m.IsSpecialName)
            .ToArray();

        foreach (var method in methods)
        {
            Assert.EndsWith("Async", method.Name);
        }
    }
}
