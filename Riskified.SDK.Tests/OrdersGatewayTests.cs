using Riskified.SDK.Model;
using Riskified.SDK.Model.OrderElements;

namespace Riskified.SDK.Tests;

/// <summary>
/// Integration tests for OrdersGateway
/// These tests require valid Riskified Sandbox credentials in testsettings.Local.json
/// Tests are marked with [Fact(Skip = ...)] by default - remove Skip to run against sandbox
/// </summary>
public class OrdersGatewayTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public OrdersGatewayTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void OrdersGateway_Initializes_WithConfiguration()
    {
        // Assert
        Assert.NotNull(_fixture.Gateway);
    }

    [Fact(Skip = "Integration test - requires valid Sandbox credentials")]
    public void Create_Order_InSandbox()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        var response = _fixture.Gateway.Create(order);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(order.Id, response.Id);
    }

    [Fact(Skip = "Integration test - requires valid Sandbox credentials")]
    public void Submit_Order_InSandbox()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act - First create, then submit
        _fixture.Gateway.Create(order);
        var response = _fixture.Gateway.Submit(order);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(order.Id, response.Id);
    }

    /// <summary>
    /// Helper method to create a test order with minimum required fields
    /// </summary>
    private Order CreateTestOrder()
    {
        var orderId = $"test-{Guid.NewGuid().ToString().Substring(0, 8)}";
        var customerId = $"customer-{Guid.NewGuid().ToString().Substring(0, 8)}";

        var customer = new Customer(
            email: "test@example.com",
            firstName: "Test",
            lastName: "User",
            id: customerId,
            ordersCount: 1,
            verifiedEmail: true,
            createdAt: DateTime.UtcNow
        );

        var billingAddress = new AddressInformation(
            firstName: "Test",
            lastName: "User",
            address1: "123 Test St",
            city: "San Francisco",
            country: "United States",
            countryCode: "US",
            phone: "415-555-1234",
            province: "California",
            provinceCode: "CA",
            zipCode: "94102"
        );

        var shippingAddress = new AddressInformation(
            firstName: "Test",
            lastName: "User",
            address1: "123 Test St",
            city: "San Francisco",
            country: "United States",
            countryCode: "US",
            phone: "415-555-1234",
            province: "California",
            provinceCode: "CA",
            zipCode: "94102"
        );

        var lineItems = new[]
        {
            new LineItem(
                title: "Test Product",
                price: 100.00,
                quantityPurchased: 1,
                productId: "test-product-123"
            )
        };

        var order = new Order(
            merchantOrderId: orderId,
            email: "test@example.com",
            customer: customer,
            billingAddress: billingAddress,
            shippingAddress: shippingAddress,
            lineItems: lineItems,
            shippingLines: new ShippingLine[] { },
            gateway: "test_gateway",
            customerBrowserIp: "192.168.1.1",
            currency: "USD",
            totalPrice: 100.00,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );

        return order;
    }
}
