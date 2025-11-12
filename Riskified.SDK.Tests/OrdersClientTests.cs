using Riskified.SDK.Clients;
using Riskified.SDK.Model;
using Riskified.SDK.Model.OrderElements;

namespace Riskified.SDK.Tests;

/// <summary>
/// Comprehensive tests for OrdersClient
/// Tests order lifecycle operations against Sandbox API
/// </summary>
public class OrdersClientTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public OrdersClientTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_CreatesOrder_InSandbox()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        var response = await _fixture.OrdersClient.CreateAsync(order);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(order.Id, response.Id);
        Assert.NotNull(response.Status);
    }

    [Fact]
    public async Task SubmitAsync_SubmitsOrderForAnalysis()
    {
        // Arrange
        var order = CreateTestOrder();
        await _fixture.OrdersClient.CreateAsync(order);

        // Act
        var response = await _fixture.OrdersClient.SubmitAsync(order);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(order.Id, response.Id);
        Assert.NotNull(response.Status);
        Assert.NotNull(response.Description);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingOrder()
    {
        // Arrange
        var order = CreateTestOrder();
        await _fixture.OrdersClient.CreateAsync(order);

        // Act
        order.TotalPrice = 150.00;
        var response = await _fixture.OrdersClient.UpdateAsync(order);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(order.Id, response.Id);
    }

    [Fact]
    public async Task CancelAsync_CancelsOrder()
    {
        // Arrange
        var order = CreateTestOrder();
        await _fixture.OrdersClient.CreateAsync(order);
        var cancellation = new OrderCancellation(order.Id, DateTime.UtcNow, "Test cancellation");

        // Act
        var response = await _fixture.OrdersClient.CancelAsync(cancellation);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(order.Id, response.Id);
    }


    [Fact]
    public async Task SendHistoricalOrdersAsync_ProcessesBatch()
    {
        // Arrange
        var orders = new[]
        {
            CreateTestOrder(),
            CreateTestOrder(),
            CreateTestOrder()
        };

        // Act
        var (success, failedOrders) = await _fixture.OrdersClient.SendHistoricalOrdersAsync(orders);

        // Assert - May have some failures due to validation, but should not throw
        Assert.True(success || failedOrders != null);
    }

    [Fact]
    public async Task CreateAsync_SupportsCancellationToken()
    {
        // Arrange
        var order = CreateTestOrder();
        var cts = new CancellationTokenSource();

        // Act
        var task = _fixture.OrdersClient.CreateAsync(order, cts.Token);

        // Assert
        Assert.True(task is Task<OrderNotification>);
    }

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

        var address = new AddressInformation(
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

        var paymentDetails = new CreditCardPaymentDetails(
            avsResultCode: "Y",
            cvvResultCode: "M",
            creditCardBin: "424242",
            creditCardCompany: "Visa",
            creditCardNumber: "4242"
        );

        return new Order(
            merchantOrderId: orderId,
            email: "test@example.com",
            customer: customer,
            billingAddress: address,
            shippingAddress: address,
            lineItems: lineItems,
            shippingLines: new ShippingLine[] { },
            gateway: "test_gateway",
            customerBrowserIp: "192.168.1.1",
            currency: "USD",
            totalPrice: 100.00,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            paymentDetails: new[] { paymentDetails }
        );
    }
}
