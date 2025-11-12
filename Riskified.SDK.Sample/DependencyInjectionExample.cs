using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Riskified.SDK.Orders;

namespace Riskified.SDK.Sample;

/// <summary>
/// Example demonstrating dependency injection pattern with Riskified SDK
/// This is the recommended approach for ASP.NET Core and modern .NET applications
/// </summary>
public static class DependencyInjectionExample
{
    public static async Task RunDependencyInjectionExample()
    {
        Console.WriteLine("=== Dependency Injection Example ===\n");

        #region Setup Dependency Injection Container

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Create service collection
        var services = new ServiceCollection();

        // Register Riskified SDK with configuration
        services.AddRiskified(configuration.GetSection("Riskified"));

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("✅ Dependency injection container configured");

        #endregion

        #region Resolve OrdersGateway from DI

        // Resolve OrdersGateway from DI container
        var gateway = serviceProvider.GetRequiredService<OrdersGateway>();
        Console.WriteLine("✅ OrdersGateway resolved from DI container\n");

        #endregion

        #region Use OrdersGateway

        Console.WriteLine("--- OrdersGateway successfully resolved from DI ---");
        Console.WriteLine($"Gateway initialized and ready to use");
        Console.WriteLine("\nIn a real ASP.NET Core app, OrdersGateway would be injected");
        Console.WriteLine("into your controllers/services via constructor injection.");
        Console.WriteLine("\nExample:");
        Console.WriteLine("  public class PaymentService(OrdersGateway gateway)");
        Console.WriteLine("  {");
        Console.WriteLine("      public async Task ProcessOrder(Order order)");
        Console.WriteLine("      {");
        Console.WriteLine("          var response = await gateway.SubmitAsync(order);");
        Console.WriteLine("          return response;");
        Console.WriteLine("      }");
        Console.WriteLine("  }");

        #endregion

        Console.WriteLine("\n=== Dependency Injection Example Complete ===");
        Console.WriteLine("\nThis pattern is recommended for ASP.NET Core applications where");
        Console.WriteLine("OrdersGateway is injected into controllers/services via constructor.\n");
    }

    public static void ShowDependencyInjectionPatterns()
    {
        Console.WriteLine("=== Dependency Injection Patterns ===\n");

        Console.WriteLine("Pattern 1: Configuration from appsettings.json");
        Console.WriteLine("------------------------------------------------");
        Console.WriteLine("// Program.cs / Startup.cs");
        Console.WriteLine("services.AddRiskified(configuration.GetSection(\"Riskified\"));");
        Console.WriteLine();

        Console.WriteLine("Pattern 2: Configuration from code");
        Console.WriteLine("-----------------------------------");
        Console.WriteLine("services.AddRiskified(options =>");
        Console.WriteLine("{");
        Console.WriteLine("    options.MerchantDomain = \"shop.myshopify.com\";");
        Console.WriteLine("    options.MerchantAuthenticationToken = \"token\";");
        Console.WriteLine("    options.Environment = RiskifiedEnvironment.Production;");
        Console.WriteLine("});");
        Console.WriteLine();

        Console.WriteLine("Pattern 3: Using in a controller/service");
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine("public class PaymentController : ControllerBase");
        Console.WriteLine("{");
        Console.WriteLine("    private readonly OrdersGateway _riskified;");
        Console.WriteLine();
        Console.WriteLine("    public PaymentController(OrdersGateway riskified)");
        Console.WriteLine("    {");
        Console.WriteLine("        _riskified = riskified;");
        Console.WriteLine("    }");
        Console.WriteLine();
        Console.WriteLine("    public async Task<IActionResult> ProcessOrder(Order order)");
        Console.WriteLine("    {");
        Console.WriteLine("        var response = await _riskified.SubmitAsync(order);");
        Console.WriteLine("        return Ok(response);");
        Console.WriteLine("    }");
        Console.WriteLine("}");
        Console.WriteLine();

        Console.WriteLine("Pattern 4: Manual instantiation (backward compatible)");
        Console.WriteLine("------------------------------------------------------");
        Console.WriteLine("// Still works! No DI required");
        Console.WriteLine("var gateway = new OrdersGateway(env, authToken, domain);");
        Console.WriteLine("var response = await gateway.CreateAsync(order);");
        Console.WriteLine();
    }
}
