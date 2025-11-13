using Riskified.SDK.Exceptions;
using Riskified.SDK.Model;

namespace Riskified.SDK.Tests;

public class ModelValidationTests
{
    [Fact]
    public void Order_Validate_ThrowsOnMissingPaymentDetails()
    {
        var order = new Order("test", "test@example.com", null, null, null, new Model.OrderElements.LineItem[] { }, null, "gw", "ip", "USD", 100, System.DateTime.UtcNow, System.DateTime.UtcNow, paymentDetails: null);
        Assert.Throws<OrderFieldBadFormatException>(() => order.Validate(Utils.Validations.All));
    }
}
