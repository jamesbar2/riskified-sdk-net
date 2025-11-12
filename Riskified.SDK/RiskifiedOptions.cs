using Riskified.SDK.Utils;

namespace Riskified.SDK
{
    /// <summary>
    /// Configuration options for Riskified SDK
    /// Used with IOptions pattern for dependency injection
    /// </summary>
    public class RiskifiedOptions
    {
        /// <summary>
        /// Configuration section name in appsettings.json
        /// </summary>
        public const string SectionKey = "Riskified";

        /// <summary>
        /// The Riskified environment to connect to (Sandbox, Production, Debug)
        /// </summary>
        public RiskifiedEnvironment Environment { get; set; } = RiskifiedEnvironment.Sandbox;

        /// <summary>
        /// The merchant's shop domain as registered with Riskified
        /// </summary>
        public string MerchantDomain { get; set; }

        /// <summary>
        /// The merchant's authentication token
        /// </summary>
        public string MerchantAuthenticationToken { get; set; }

        /// <summary>
        /// Validation mode to use (All, Weak, Skip)
        /// </summary>
        public Validations ValidationMode { get; set; } = Validations.All;

        /// <summary>
        /// HTTP client timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum connections per server
        /// </summary>
        public int MaxConnectionsPerServer { get; set; } = 10;
    }
}
