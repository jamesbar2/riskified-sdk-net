using Microsoft.Extensions.Configuration;

namespace Riskified.SDK.Sample;

/// <summary>
/// Helper class for loading configuration from appsettings.json
/// Replaces legacy ConfigurationManager/App.config pattern
/// </summary>
public static class ConfigurationHelper
{
    private static IConfiguration _configuration;

    /// <summary>
    /// Gets the configuration instance, building it if necessary
    /// </summary>
    public static IConfiguration Configuration
    {
        get
        {
            if (_configuration == null)
            {
                _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();
            }
            return _configuration;
        }
    }

    /// <summary>
    /// Gets a configuration value from the Riskified section
    /// </summary>
    public static string GetRiskifiedSetting(string key)
    {
        return Configuration[$"Riskified:{key}"]
            ?? throw new InvalidOperationException($"Missing required configuration: Riskified:{key}");
    }

    /// <summary>
    /// Gets a configuration value from the Riskified section with a default value
    /// </summary>
    public static string GetRiskifiedSetting(string key, string defaultValue)
    {
        return Configuration[$"Riskified:{key}"] ?? defaultValue;
    }
}
