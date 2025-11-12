using Riskified.SDK.Clients;
using Riskified.SDK.Model.OrderElements;

namespace Riskified.SDK.Tests;

/// <summary>
/// Tests for OtpClient
/// Tests OTP recovery operations
/// </summary>
public class OtpClientTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public OtpClientTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void OtpClient_Initializes()
    {
        var client = new OtpClient(_fixture.Environment, _fixture.AuthToken, _fixture.MerchantDomain);
        Assert.NotNull(client);
    }


    [Fact]
    public void OtpClient_OnlyHasAsyncMethods()
    {
        var methods = typeof(OtpClient).GetMethods()
            .Where(m => m.IsPublic && m.DeclaringType == typeof(OtpClient) && !m.IsSpecialName)
            .ToArray();

        foreach (var method in methods)
        {
            Assert.EndsWith("Async", method.Name);
        }
    }
}
