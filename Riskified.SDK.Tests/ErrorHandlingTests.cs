using Riskified.SDK.Clients;
using Riskified.SDK.Exceptions;
using Riskified.SDK.Utils;

namespace Riskified.SDK.Tests;

public class ErrorHandlingTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public ErrorHandlingTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void RiskifiedOptions_ThrowsOnMissingAuthToken()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new RiskifiedOptions
        {
            MerchantDomain = "test.com",
            MerchantAuthenticationToken = null
        });

        Assert.Throws<ArgumentException>(() => new OrdersClient(options));
    }

    [Fact]
    public void OrdersClient_AcceptsNullHttpClientFactory()
    {
        var client = new OrdersClient(RiskifiedEnvironment.Sandbox, "token", "domain", httpClientFactory: null);
        Assert.NotNull(client);
    }
}
