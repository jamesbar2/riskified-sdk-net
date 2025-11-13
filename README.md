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

### Simple Usage

```csharp
using Riskified.SDK.Orders;
using Riskified.SDK.Utils;

// Initialize gateway
var gateway = new OrdersGateway(
    RiskifiedEnvironment.Sandbox,
    authToken: "your-auth-token",
    shopDomain: "your-shop.myshopify.com"
);

// Submit order asynchronously (recommended)
var order = new Order(...);
var response = await gateway.SubmitAsync(order);

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

**3. Inject into your services:**

```csharp
public class PaymentService
{
    private readonly OrdersGateway _riskified;

    public PaymentService(OrdersGateway riskified)
    {
        _riskified = riskified;
    }

    public async Task<OrderNotification> ProcessOrder(Order order)
    {
        var response = await _riskified.SubmitAsync(order);
        return response;
    }
}
```

`OrdersGateway` is comfortable being used as a singleton in this manner.

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

## API Methods

All methods have both synchronous and async variants. **Async methods are recommended** for production.

### Order Operations

```csharp
await gateway.CreateAsync(order);        // Create without submission
await gateway.SubmitAsync(order);        // Submit for analysis
await gateway.UpdateAsync(order);        // Update existing order
await gateway.DecideAsync(order);        // Get synchronous decision
await gateway.CancelAsync(cancellation); // Cancel order
await gateway.PartlyRefundAsync(refund); // Partial refund
await gateway.FulfillAsync(fulfillment); // Mark fulfilled
```

### Checkout Operations

```csharp
await gateway.CheckoutAsync(orderCheckout);
await gateway.AdviseAsync(orderCheckout);
await gateway.CheckoutDeniedAsync(orderCheckoutDenied);
```

### Account Actions

```csharp
await gateway.LoginAsync(login);
await gateway.CustomerCreateAsync(customerCreate);
await gateway.CustomerUpdateAsync(customerUpdate);
await gateway.LogoutAsync(logout);
// And more...
```

### Batch Operations

```csharp
var orders = new[] { order1, order2, order3 };
var (success, failedOrders) = await gateway.SendHistoricalOrdersAsync(orders);
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

## Webhook Handling

Riskified sends fraud decisions back to your application via webhooks. Use the `WebhookValidator` utility to validate and parse webhooks in your ASP.NET Core application.

### ASP.NET Core Minimal API

```csharp
using Riskified.SDK.Webhooks;

app.MapPost("/webhooks/riskified", async (HttpRequest request) =>
{
    try
    {
        var notification = await WebhookValidator.ValidateAndParseAsync(
            request,
            authToken: configuration["Riskified:MerchantAuthenticationToken"],
            expectedShopDomain: configuration["Riskified:MerchantDomain"]
        );

        // Handle the notification
        Console.WriteLine($"Order {notification.Id}: {notification.Status}");
        Console.WriteLine($"Description: {notification.Description}");

        // Process based on status
        if (notification.Status == "approved")
        {
            // Fulfill order
        }
        else if (notification.Status == "declined")
        {
            // Cancel order
        }

        return Results.Ok("Webhook received");
    }
    catch (RiskifiedAuthenticationException ex)
    {
        return Results.Unauthorized();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
});
```

### ASP.NET Core Controller

```csharp
using Riskified.SDK.Webhooks;

[ApiController]
[Route("webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public WebhookController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("riskified")]
    public async Task<IActionResult> HandleRiskifiedWebhook()
    {
        try
        {
            var notification = await WebhookValidator.ValidateAndParseAsync(
                Request,
                authToken: _configuration["Riskified:MerchantAuthenticationToken"],
                expectedShopDomain: _configuration["Riskified:MerchantDomain"]
            );

            // Process notification
            await ProcessNotification(notification);

            return Ok();
        }
        catch (RiskifiedAuthenticationException)
        {
            return Unauthorized();
        }
    }

    private async Task ProcessNotification(OrderNotification notification)
    {
        // Your business logic here
    }
}
```

### HMAC Validation Only

If you need manual HMAC validation:

```csharp
using Riskified.SDK.Webhooks;

// Validate HMAC signature
var body = await ReadBodyAsync(request);
var providedHmac = request.Headers["X-RISKIFIED-HMAC-SHA256"];
var isValid = WebhookValidator.ValidateHmac(
    body,
    providedHmac,
    authToken: "your-auth-token"
);

if (!isValid)
{
    return Results.Unauthorized();
}
```
