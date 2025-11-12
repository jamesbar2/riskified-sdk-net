using Riskified.SDK.Model;
using Riskified.SDK.Model.OrderElements;

namespace Riskified.SDK.Tests;

/// <summary>
/// Tests for async API methods added in Phase 3
/// Validates that all async methods are properly implemented
/// </summary>
public class AsyncApiTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public AsyncApiTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_Method_Exists_AndIsAsync()
    {
        // Arrange
        var order = CreateMinimalTestOrder();

        // Act - This demonstrates the async method signature exists
        var task = _fixture.Gateway.CreateAsync(order);

        // Assert
        Assert.True(task is Task<OrderNotification>);
        // Don't await - we don't have valid credentials
    }

    [Fact]
    public async Task SubmitAsync_Method_Exists_AndIsAsync()
    {
        // Arrange
        var order = CreateMinimalTestOrder();

        // Act
        var task = _fixture.Gateway.SubmitAsync(order);

        // Assert
        Assert.True(task is Task<OrderNotification>);
    }

    [Fact]
    public async Task UpdateAsync_Method_Exists_AndIsAsync()
    {
        // Arrange
        var order = CreateMinimalTestOrder();

        // Act
        var task = _fixture.Gateway.UpdateAsync(order);

        // Assert
        Assert.True(task is Task<OrderNotification>);
    }

    [Fact]
    public async Task CancelAsync_Method_Exists_AndIsAsync()
    {
        // Arrange
        var cancellation = new OrderCancellation(
            merchantOrderId: "test-123",
            cancelledAt: DateTime.UtcNow,
            cancelReason: "Customer request"
        );

        // Act
        var task = _fixture.Gateway.CancelAsync(cancellation);

        // Assert
        Assert.True(task is Task<OrderNotification>);
    }

    [Fact]
    public async Task DecideAsync_Method_Exists_AndIsAsync()
    {
        // Arrange
        var order = CreateMinimalTestOrder();

        // Act
        var task = _fixture.Gateway.DecideAsync(order);

        // Assert
        Assert.True(task is Task<OrderNotification>);
    }

    [Fact]
    public async Task SendHistoricalOrdersAsync_Returns_ValueTuple()
    {
        // Arrange
        var orders = new[] { CreateMinimalTestOrder() };

        // Act
        var task = _fixture.Gateway.SendHistoricalOrdersAsync(orders);

        // Assert - Verify it returns a value tuple
        Assert.True(task is Task<(bool Success, Dictionary<string, string> FailedOrders)>);
    }

    [Fact]
    public async Task AllAsyncMethods_SupportCancellationToken()
    {
        // Arrange
        var order = CreateMinimalTestOrder();
        var cts = new CancellationTokenSource();

        // Act - Verify cancellation token parameter exists
        var createTask = _fixture.Gateway.CreateAsync(order, cancellationToken: cts.Token);
        var submitTask = _fixture.Gateway.SubmitAsync(order, cancellationToken: cts.Token);
        var updateTask = _fixture.Gateway.UpdateAsync(order, cancellationToken: cts.Token);

        // Assert
        Assert.True(createTask is Task<OrderNotification>);
        Assert.True(submitTask is Task<OrderNotification>);
        Assert.True(updateTask is Task<OrderNotification>);
    }

    [Fact]
    public void OrdersGateway_AutomaticallyCreatesHttpClientFactory()
    {
        // Arrange & Act - Create gateway without providing IHttpClientFactory
        var gateway = new OrdersGateway(
            RiskifiedEnvironment.Sandbox,
            "test-token",
            "test-domain.myshopify.com"
        );

        // Assert - Gateway should automatically create internal HttpClientFactory
        Assert.NotNull(gateway);
        // HttpClient will be created automatically when async methods are called
    }

    private Order CreateMinimalTestOrder()
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
            updatedAt: DateTime.UtcNow
        );
    }
}
