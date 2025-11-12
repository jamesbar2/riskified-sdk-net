# Riskified SDK Tests

Modern test suite for the Riskified .NET SDK using xUnit.

## Test Framework

- **xUnit 2.9.3** - Modern .NET test framework
- **coverlet** - Code coverage reporting
- **GitHubActionsTestLogger** - CI/CD integration
- **No mocking libraries** - Integration-focused tests using real Sandbox API

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run With Code Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~ConfigurationTests"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~ConfigurationTests.Configuration_Loads_Successfully"
```

## Configuration

Tests use `testsettings.json` for base configuration. For credentials, you have three secure options:

### Option 1: User Secrets (Recommended ✅)

Most secure - credentials stored outside the repository:

```bash
cd Riskified.SDK.Tests
dotnet user-secrets set "Riskified:MerchantDomain" "your-domain.myshopify.com"
dotnet user-secrets set "Riskified:MerchantAuthenticationToken" "your-sandbox-token"
dotnet user-secrets set "Riskified:RiskifiedEnvironment" "Sandbox"
```

Secrets are stored in: `~/.microsoft/usersecrets/riskified-sdk-tests-a1b2c3d4/`

To view your secrets:
```bash
dotnet user-secrets list
```

To remove a secret:
```bash
dotnet user-secrets remove "Riskified:MerchantAuthenticationToken"
```

### Option 2: Environment Variables

Good for CI/CD pipelines:

```bash
export Riskified__MerchantDomain="your-domain.myshopify.com"
export Riskified__MerchantAuthenticationToken="your-token"
export Riskified__RiskifiedEnvironment="Sandbox"
```

### Option 3: Local JSON File (Not Recommended)

Create `testsettings.Local.json` (git-ignored):

```json
{
  "Riskified": {
    "MerchantDomain": "your-shop-domain.myshopify.com",
    "MerchantAuthenticationToken": "your-sandbox-auth-token",
    "RiskifiedEnvironment": "Sandbox"
  }
}
```

⚠️ **Warning:** This file is git-ignored but still exists on disk. Use User Secrets instead.

### Configuration Priority

Settings are loaded in this order (later sources override earlier ones):
1. `testsettings.json` (base settings, checked into git)
2. `testsettings.Local.json` (local overrides, git-ignored)
3. Environment variables
4. User Secrets (highest priority)

## Test Categories

### ConfigurationTests
✅ **Always Run** - No credentials required

Tests the modernized configuration system:
- Configuration loading from JSON
- Environment parsing
- SDK initialization

### SerializationTests
✅ **Always Run** - No credentials required

Tests JSON serialization with Newtonsoft.Json 13.x:
- Model serialization/deserialization
- JsonProperty attribute mappings
- Round-trip data integrity

### OrdersGatewayTests
⏭️ **Skipped by Default** - Requires Sandbox credentials

Integration tests against Riskified Sandbox API:
- Order creation
- Order submission
- API responses

**To enable:** Remove `Skip` attribute from test methods and add credentials to `testsettings.Local.json`

## Test Patterns

### Fixture Pattern
Following xUnit best practices with `IAsyncLifetime`:

```csharp
public class MyTests : IClassFixture<RiskifiedTestFixture>
{
    private readonly RiskifiedTestFixture _fixture;

    public MyTests(RiskifiedTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void My_Test()
    {
        // Use _fixture.Gateway, _fixture.Configuration, etc.
    }
}
```

### Integration Test Pattern
```csharp
[Fact(Skip = "Integration test - requires valid Sandbox credentials")]
public void My_Integration_Test()
{
    // Test implementation
}
```

Remove the `Skip` parameter when you have valid credentials.

## Validating Phase 1 Changes

The test suite validates all Phase 1 modernization work:

✅ **Multi-targeting** - Tests run on .NET 8
✅ **Modern Configuration** - IConfiguration with appsettings.json
✅ **Cross-Platform** - Runs on Linux/macOS/Windows
✅ **Updated Dependencies** - Newtonsoft.Json 13.x serialization
✅ **SDK Initialization** - OrdersGateway creates successfully

## CI/CD Integration

Tests include GitHubActionsTestLogger for automated CI:

```yaml
# In GitHub Actions workflow:
- name: Run Tests
  run: dotnet test --logger GitHubActions
```

## Code Coverage

Generate HTML coverage report:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
reportgenerator -reports:coverage.cobertura.xml -targetdir:coverage-report
```

## Next Steps

As the SDK evolves through phases 2-6, add tests for:
- Phase 2: HttpClient integration tests
- Phase 3: Async method tests
- Phase 4: Dependency injection tests
- Phase 5: Logging integration tests
- Phase 6: Nullable reference type validation
