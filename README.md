# Riskified .NET SDK

Modern .NET SDK for the Riskified fraud prevention platform. Supports .NET Standard 2.0, .NET 6, and .NET 8 with full async/await, HttpClient, and dependency injection support.

**Version:** 5.0.0
**API Documentation:** [apiref.riskified.com](http://apiref.riskified.com)

## Features

- ✅ **Cross-Platform:** Linux, macOS, Windows (including ARM64)
- ✅ **Modern HTTP:** HttpClient + IHttpClientFactory (no deprecated APIs)
- ✅ **Async/Await:** Full async API surface for non-blocking I/O
- ✅ **Dependency Injection:** IOptions and IServiceCollection support
- ✅ **Multi-Targeting:** netstandard2.0, net6.0, net8.0
- ✅ **Production Ready:** Validated against Riskified Sandbox API
- ✅ **Comprehensive Tests:** Including live integration tests

## Installation

```bash
dotnet add package Riskified.SDK
```

## Quick Start

### Simple Usage (Recommended)

```csharp
using Riskified.SDK.Clients;
using Riskified.SDK.Utils;

// Initialize OrdersClient for order operations
var ordersClient = new OrdersClient(
    RiskifiedEnvironment.Sandbox,
    authToken: "your-auth-token",
    shopDomain: "your-shop.myshopify.com"
);

// Submit order asynchronously
var order = new Order(...);
var response = await ordersClient.SubmitAsync(order);

Console.WriteLine($"Order {response.Id}: {response.Status}");
```

### ASP.NET Core with Dependency Injection (Recommended)

**1. Configure in `appsettings.json`:**

```json
{
  "Riskified": {
    "MerchantDomain": "your-shop.myshopify.com",
    "MerchantAuthenticationToken": "your-auth-token",
    "Environment": "Production"
  }
}
```

**2. Register services in `Program.cs`:**

```csharp
builder.Services.AddRiskified(
    builder.Configuration.GetSection("Riskified")
);
```

This will automatically bind options from the configuration section, configure a named `HttpClient` for Riskified with proper connection pooling, and register `OrdersGateway` as a singleton.

**3. Inject specialized clients into your services:**

```csharp
using Riskified.SDK.Clients;

public class PaymentService
{
    private readonly OrdersClient _orders;
    private readonly CheckoutClient _checkout;

    public PaymentService(OrdersClient orders, CheckoutClient checkout)
    {
        _orders = orders;
        _checkout = checkout;
    }

    public async Task<OrderNotification> ProcessPayment(Order order)
    {
        // Pre-checkout screening (optional)
        var checkoutResponse = await _checkout.AdviseAsync(orderCheckout);

        // Submit order for fraud analysis
        var response = await _orders.SubmitAsync(order);
        return response;
    }
}
```

All specialized clients are comfortable being used as singletons in this manner.

## Configuration Options

### IHttpClientFactory

The SDK supports `IHttpClientFactory` for correct usage of `HttpClient`, as described in [Microsoft's documentation](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests).

When you call `.AddRiskified()`, it automatically configures a named `HttpClient` with proper connection pooling and lifecycle management. If you need to customize your DI structure, you can call `services.AddRiskifiedHttpClient()` separately.

### IOptions Support

The SDK supports configuration from any source via the `IOptions` pattern. You can provide any configuration section to `.AddRiskified()` and options will be automatically bound.

Alternatively, configure options manually:

```csharp
services.Configure<RiskifiedOptions>(options =>
{
    options.MerchantDomain = "your-shop.myshopify.com";
    options.MerchantAuthenticationToken = "your-token";
    options.Environment = RiskifiedEnvironment.Production;
});
services.AddSingleton<OrdersGateway>();
```

## Specialized Clients (Recommended)

The SDK provides focused clients for each API domain following the Single Responsibility Principle.

### OrdersClient - Order Lifecycle Operations

```csharp
using Riskified.SDK.Clients;

var ordersClient = new OrdersClient(env, authToken, shopDomain);

await ordersClient.CreateAsync(order);        // Create without submission
await ordersClient.SubmitAsync(order);        // Submit for analysis
await ordersClient.UpdateAsync(order);        // Update existing order
await ordersClient.DecideAsync(order);        // Get synchronous decision
await ordersClient.CancelAsync(cancellation); // Cancel order
await ordersClient.PartlyRefundAsync(refund); // Partial refund
await ordersClient.FulfillAsync(fulfillment); // Mark fulfilled
await ordersClient.DecisionAsync(decision);   // Report merchant decision
await ordersClient.ChargebackAsync(chargeback); // Report chargeback

// Batch operations
var (success, failedOrders) = await ordersClient.SendHistoricalOrdersAsync(orders);
```

### CheckoutClient - Pre-Checkout Operations

```csharp
using Riskified.SDK.Clients;

var checkoutClient = new CheckoutClient(env, authToken, shopDomain);

await checkoutClient.CheckoutAsync(orderCheckout);      // Process checkout
await checkoutClient.AdviseAsync(orderCheckout);        // Pre-checkout screening
await checkoutClient.CheckoutDeniedAsync(checkoutDenied); // Report denial
```

### AccountClient - Account Security Operations

```csharp
using Riskified.SDK.Clients;

var accountClient = new AccountClient(env, authToken, shopDomain);

await accountClient.LoginAsync(login);
await accountClient.LogoutAsync(logout);
await accountClient.CustomerCreateAsync(customerCreate);
await accountClient.CustomerUpdateAsync(customerUpdate);
await accountClient.ResetPasswordRequestAsync(resetPasswordRequest);
await accountClient.WishlistChangesAsync(wishlistChanges);
await accountClient.RedeemAsync(redeem);
await accountClient.CustomerReachOutAsync(customerReachOut);
```

### DecoClient & OtpClient

```csharp
// Deco payment operations
var decoClient = new DecoClient(env, authToken, shopDomain);
await decoClient.EligibleAsync(orderIdOnly);
await decoClient.OptInAsync(orderIdOnly);

// OTP recovery operations
var otpClient = new OtpClient(env, authToken, shopDomain);
await otpClient.InitiateOtpAsync(otpInitiate);
```

## Legacy API (Backward Compatibility)

**OrdersGateway** is still available but marked as obsolete. Use specialized clients instead.

```csharp
[Obsolete] // Will show deprecation warning
var gateway = new OrdersGateway(env, authToken, shopDomain);

// Synchronous methods still work (delegates to clients)
var response = gateway.Create(order);
var response = gateway.Submit(order);
```

## Examples

The SDK includes comprehensive examples in `Riskified.SDK.Sample`:

```bash
# Async example (recommended)
dotnet run --project Riskified.SDK.Sample -- async

# Dependency injection example
dotnet run --project Riskified.SDK.Sample -- di

# Show DI patterns
dotnet run --project Riskified.SDK.Sample -- di-patterns

# Legacy synchronous example
dotnet run --project Riskified.SDK.Sample

# Run all endpoints
dotnet run --project Riskified.SDK.Sample -- run_all
```

## Testing

```bash
dotnet test
```

For integration tests, see `Riskified.SDK.Tests/USER_SECRETS_DEMO.md` for secure credential setup.

## Framework Support

| Framework | Supported |
|-----------|-----------|
| .NET 8 | ✅ |
| .NET 6 | ✅ |
| .NET Standard 2.0 | ✅ |
| .NET Framework 4.6.1+ | ✅ |
| .NET Framework 4.5.1 | ❌ (use v4.x) |

## Migration from v4.x to v5.0

See `MODERNIZATION_ROADMAP.md` for complete migration guide.

**Key Changes:**
- Minimum framework: .NET Framework 4.6.1 or .NET Core 2.0+
- Configuration: App.config → appsettings.json
- New: Async/await methods (recommended)
- New: Dependency injection support
- Updated: Newtonsoft.Json 13.0.3

**Backward Compatibility:**
All existing synchronous methods still work. Async methods are additive.

## Documentation

- **MODERNIZATION_ROADMAP.md** - Modernization plan and progress
- **Riskified.SDK.Tests/README.md** - Testing guide
- **Riskified.SDK.Tests/USER_SECRETS_DEMO.md** - Secure credentials setup
