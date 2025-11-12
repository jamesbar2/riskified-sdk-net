# Riskified SDK .NET Modernization Roadmap

## Executive Summary

This document outlines the comprehensive modernization plan for the Riskified .NET SDK. The SDK currently targets .NET Framework 4.5.1 (released 2013) and uses deprecated APIs. This modernization effort will bring the SDK up to current .NET standards, enabling support for .NET 6+, async/await patterns, dependency injection, and modern HTTP infrastructure.

## Current State Analysis

### Critical Issues Identified

**Framework & Dependencies:**
- Target Framework: .NET Framework 4.5.1 (11+ years old)
- Newtonsoft.Json: v9.0.1 (2016, 6 major versions behind)
- No multi-targeting support
- Legacy packages.config format

**HTTP Infrastructure:**
- Uses WebRequest/HttpWebResponse (deprecated, removed in .NET Core)
- Synchronous-only HTTP operations
- No HttpClient or IHttpClientFactory support
- Manual connection management

**API Design:**
- Zero async/await support (0 async methods in entire codebase)
- Blocking I/O operations throughout
- No Task-based Asynchronous Pattern (TAP) implementation

**Architecture:**
- No dependency injection support
- Static service locator pattern for logging (anti-pattern)
- App.config-based configuration (not portable to .NET Core+)
- Custom logging interface instead of Microsoft.Extensions.Logging

**Testing:**
- No test projects despite test packages present
- No unit or integration tests
- Manual testing only via sample application

### Codebase Statistics

- **103 C# files**, ~8,000+ lines of code
- **80+ domain model classes**
- **6 custom exception types**
- **0 async methods**
- **0 unit tests**
- **19 files** with unused Task imports

## Versioning Strategy

This modernization requires breaking changes that make the SDK incompatible with .NET Framework 4.5.1. We're adopting a clean break approach:

**Version 2.0.0** - Modern .NET SDK
- Target Frameworks: `netstandard2.0;net6.0;net8.0`
- Minimum requirement: .NET Framework 4.6.1 or .NET Core 2.0+
- Modern APIs: HttpClient, async/await, dependency injection

**Version 1.x** - Legacy (archived, unsupported)
- Remains available for .NET Framework 4.5.1 customers
- No new features or bug fixes
- Customers should upgrade to 2.0

### Backward Compatibility Impact

| Customer's Framework | Can Use v1.x | Can Use v2.0 | Recommendation |
|---------------------|--------------|--------------|----------------|
| .NET Framework 4.5.1 | ✅ Yes | ❌ No | Upgrade to .NET 4.6.1+ |
| .NET Framework 4.6.1+ | ✅ Yes | ✅ Yes | Migrate to v2.0 |
| .NET Core 2.0+ | ❌ No | ✅ Yes | Use v2.0 |
| .NET 5, 6, 7, 8 | ❌ No | ✅ Yes | Use v2.0 |

**Rationale:** .NET Framework 4.5.1 reached end-of-life in January 2016 (9 years ago). Major SDK vendors (AWS, Azure, etc.) dropped 4.5.1 support years ago. Customers still on 4.5.1 have significant technical debt and security risks.

## Modernization Strategy: 6-Phase Approach

### Phase 1: Foundation - Update Target Framework
**Branch:** `jbarnett/1-update-net`
**PR Target:** `master`

**Objectives:**
- Migrate from .NET Framework 4.5.1 to multi-targeting
- Target frameworks: `netstandard2.0;net6.0;net8.0`
- Convert to SDK-style project format
- Migrate packages.config to PackageReference
- Update Newtonsoft.Json to v13.x
- Enable nullable reference types
- Ensure backward compatibility

**Files Affected:**
- `Riskified.SDK.csproj` - Convert to SDK-style, add multi-targeting
- `Riskified.SDK.Sample.csproj` - Update to .NET 6+
- `packages.config` - Remove (migrate to PackageReference)
- All source files - Add nullable annotations as needed

**Complexity:** Medium
**Risk:** Low (primarily build system changes)
**Estimated Effort:** 1-2 days

---

### Phase 2: HTTP Infrastructure - HttpClient Migration
**Branch:** `jbarnett/2-httpclient`
**PR Target:** `jbarnett/1-update-net`

**Objectives:**
- Replace WebRequest/HttpWebResponse with HttpClient
- Implement IHttpClientFactory pattern
- Add named HttpClient registration
- Configure automatic decompression
- Maintain HMAC-SHA256 authentication logic
- Keep existing synchronous API surface (for now)
- Implement proper disposal patterns

**Key Changes:**
```csharp
// Before: WebRequest.CreateHttp(url)
// After:  _httpClientFactory.CreateClient("RiskifiedClient")
```

**Files Affected:**
- `Utils/HttpUtils.cs` - Complete rewrite of HTTP layer
- **New:** `RiskifiedServiceCollectionExtensions.cs` - DI registration
- `Orders/OrdersGateway.cs` - Update to use new HTTP layer

**Complexity:** High
**Risk:** Medium (core infrastructure change)
**Estimated Effort:** 3-4 days

---

### Phase 3: Async/Await Support
**Branch:** `jbarnett/3-async-await`
**PR Target:** `jbarnett/2-httpclient`

**Objectives:**
- Add async variants of all public API methods
- Implement `async Task<T>` return types throughout
- Use `ConfigureAwait(false)` for library code
- Keep synchronous methods for backward compatibility (mark obsolete)
- Update HTTP layer to use async HttpClient methods
- Modernize notification handler async patterns

**API Pattern:**
```csharp
// Existing: public OrderNotification Create(Order order)
// New:      public Task<OrderNotification> CreateAsync(Order order)

// Existing: PostObject<T>(...)
// New:      Task<T> PostObjectAsync<T>(...) with async/await
```

**Files Affected:**
- `Orders/OrdersGateway.cs` - Add *Async methods for all operations (Create, Update, Submit, Cancel, etc.)
- `Utils/HttpUtils.cs` - Add async variants of all HTTP methods
- `Notifications/NotificationHandler.cs` - Modernize async patterns
- Sample applications - Update to demonstrate async usage

**Complexity:** High
**Risk:** Low (additive changes, maintains backward compatibility)
**Estimated Effort:** 3-5 days

---

### Phase 4: Dependency Injection & Configuration
**Branch:** `jbarnett/4-dependency-injection`
**PR Target:** `jbarnett/3-async-await`

**Objectives:**
- Create `RiskifiedOptions` class with IOptions pattern
- Implement service collection extensions
- Support multiple initialization patterns
- Add configuration from appsettings.json
- Register services as Singleton
- Maintain backward compatibility with direct instantiation

**Initialization Patterns:**
```csharp
// Pattern 1: Simple (existing, maintained)
var gateway = new OrdersGateway(env, authToken, shopDomain);

// Pattern 2: Constructor with all options
var gateway = new OrdersGateway(
    environment: RiskifiedEnvironment.Production,
    authToken: "token",
    shopDomain: "shop.myshopify.com");

// Pattern 3: Dependency Injection (new, recommended)
services.AddRiskified(configuration.GetSection("Riskified"));
// Inject: public MyService(OrdersGateway gateway) { ... }
```

**New Files:**
- `RiskifiedOptions.cs` - Configuration POCO
- `RiskifiedServiceCollectionExtensions.cs` - DI registration methods

**Files Modified:**
- `Orders/OrdersGateway.cs` - Add constructor overloads for DI
- Sample application - Add appsettings.json, demonstrate DI usage

**Complexity:** Medium
**Risk:** Low (additive, maintains backward compatibility)
**Estimated Effort:** 2-3 days

---

### Phase 5: Modern Logging Infrastructure
**Branch:** `jbarnett/5-logging`
**PR Target:** `jbarnett/4-dependency-injection`

**Objectives:**
- Replace custom ILogger with Microsoft.Extensions.Logging.ILogger
- Remove static LoggingServices service locator
- Inject ILogger<T> via dependency injection
- Add structured logging with proper log levels
- Support NullLogger for non-DI scenarios
- Maintain backward compatibility via adapter pattern

**Logging Pattern:**
```csharp
// Before: LoggingServices.Debug("message")
// After:  _logger.LogDebug("message")

// Constructor:
public OrdersGateway(
    ...,
    ILogger<OrdersGateway>? logger = null)
{
    _logger = logger ?? NullLogger<OrdersGateway>.Instance;
}
```

**Files Affected:**
- `Logging/ILogger.cs` - Mark as obsolete
- `Logging/LoggingServices.cs` - Mark as obsolete
- **New:** `Logging/LegacyLoggerAdapter.cs` - Backward compatibility adapter
- `Orders/OrdersGateway.cs` - Inject ILogger<T>
- `Utils/HttpUtils.cs` - Add structured logging
- `Notifications/NotificationHandler.cs` - Add logging support

**Complexity:** Medium
**Risk:** Low (primarily internal changes, adapter maintains compatibility)
**Estimated Effort:** 2-3 days

---

### Phase 6: Serialization & Final Modernization
**Branch:** `jbarnett/6-serialization`
**PR Target:** `jbarnett/5-logging`

**Objectives:**
- Evaluate System.Text.Json vs Newtonsoft.Json upgrade
- Update JSON serialization settings and patterns
- Add source generators (if using System.Text.Json)
- Enable nullable reference types throughout
- Add implicit usings
- Clean up unused imports (19 files with unused Task imports)
- Add comprehensive XML documentation comments
- Standardize code patterns and formatting

**Decision Point:**
- **Option A:** Upgrade Newtonsoft.Json to v13.x (safer, less work, maintains compatibility)
- **Option B:** Migrate to System.Text.Json (modern, built-in, but more migration work)

**Files Affected:**
- All 80+ model classes - Update serialization attributes if migrating
- `Utils/HttpUtils.cs` - Update serialization calls
- **New:** `GlobalUsings.cs` - Add implicit usings
- All source files - Enable nullable reference types, add XML docs

**Complexity:** Medium-High
**Risk:** Medium (serialization changes require extensive testing)
**Estimated Effort:** 3-4 days

---

## Branch Dependency Chain

```
master
  └─> jbarnett/1-update-net (PR to master)
        └─> jbarnett/2-httpclient (PR to jbarnett/1-update-net)
              └─> jbarnett/3-async-await (PR to jbarnett/2-httpclient)
                    └─> jbarnett/4-dependency-injection (PR to jbarnett/3-async-await)
                          └─> jbarnett/5-logging (PR to jbarnett/4-dependency-injection)
                                └─> jbarnett/6-serialization (PR to jbarnett/5-logging)
```

Each branch will have its own pull request, making changes easily reviewable and understandable. PRs will be chained together, with each building on the previous.

## Modern .NET SDK Patterns

The following patterns represent industry best practices for modern .NET SDKs:

### 1. HttpClient Management Pattern
- **Use IHttpClientFactory** with named client registration
- Enable automatic decompression (GZip/Deflate)
- Proper disposal handled via DI container
- Connection pooling automatically managed
- Avoid socket exhaustion issues

### 2. Async-First Design
- All I/O operations should be async
- Use `ConfigureAwait(false)` in library code
- Return `Task<T>` for external APIs
- Maintain sync methods only for backward compatibility (mark obsolete)
- Never block on async code (no `.Result` or `.Wait()`)

### 3. Configuration Pattern
- Use `IOptions<T>` with strongly-typed configuration class
- Support appsettings.json, environment variables, etc.
- Provide sensible default values
- Use named section constant for discoverability
- Support validation via DataAnnotations

### 4. Dependency Injection Pattern
- Provide extension methods on `IServiceCollection`
- Use fluent/chainable registration API
- Register HTTP services as Singleton (stateless)
- Optional constructor parameters for flexibility
- Support both DI and manual instantiation

### 5. Error Handling Pattern
- Return errors as response properties (not exceptions for API errors)
- Throw exceptions only for programming errors
- Provide graceful degradation for API unavailability
- Include detailed error information at multiple levels
- Support request ID tracking for troubleshooting

### 6. Logging Pattern
- Use structured logging with `ILogger<T>`
- Employ proper log levels (Trace, Debug, Info, Warning, Error, Critical)
- Track request/response lifecycle
- Use `NullLogger` fallback for non-DI scenarios
- Avoid logging sensitive data (credentials, PII)

### 7. Multi-Targeting Strategy
- **netstandard2.0**: Broad compatibility (.NET Framework 4.6.1+, .NET Core 2.0+)
- **net6.0**: LTS release (supported until November 2024)
- **net8.0**: Current LTS (supported until November 2026)
- Use framework-specific optimizations where beneficial
- Conditional compilation for framework-specific features

### 8. Code Quality Standards
- Enable nullable reference types
- Use implicit usings for cleaner code
- Leverage modern C# features (records, init properties, pattern matching)
- Provide comprehensive XML documentation for public APIs
- Enable code analyzers and treat warnings as errors in CI

## Benefits After Modernization

| Area | Before | After |
|------|--------|-------|
| **Framework** | .NET 4.5.1 only | netstandard2.0, net6.0, net8.0 |
| **HTTP** | WebRequest (deprecated) | HttpClient + IHttpClientFactory |
| **Performance** | Synchronous blocking | Async/await non-blocking |
| **DI Support** | None | Full Microsoft.Extensions.DI |
| **Configuration** | App.config only | IOptions + appsettings.json |
| **Logging** | Custom static service | Microsoft.Extensions.Logging |
| **Testing** | No tests | Fully testable via DI |
| **Compatibility** | .NET Framework only | .NET Framework + .NET Core/5/6/8 |
| **Maintainability** | Legacy patterns (2013) | Modern best practices (2024) |
| **Async Support** | None (0 methods) | Full async/await support |

## Success Criteria

### Phase 1 Success Criteria
- [ ] SDK builds successfully for netstandard2.0, net6.0, net8.0
- [ ] Sample application runs on .NET 6+
- [ ] All existing functionality works (backward compatibility)
- [ ] Dependencies updated to current stable versions
- [ ] No build warnings

### Phase 2 Success Criteria
- [ ] WebRequest completely removed
- [ ] All HTTP calls use HttpClient via IHttpClientFactory
- [ ] HMAC authentication still works correctly
- [ ] Connection pooling validated
- [ ] No socket exhaustion under load

### Phase 3 Success Criteria
- [ ] All public methods have async variants
- [ ] Async methods use ConfigureAwait(false)
- [ ] Sample application demonstrates async usage
- [ ] No sync-over-async anti-patterns
- [ ] Backward compatibility maintained with sync methods

### Phase 4 Success Criteria
- [ ] Services can be registered via AddRiskified()
- [ ] Configuration can be loaded from appsettings.json
- [ ] Both DI and manual instantiation work
- [ ] Sample application demonstrates DI pattern
- [ ] IOptions validation works

### Phase 5 Success Criteria
- [ ] Microsoft.Extensions.Logging integrated
- [ ] Structured logging throughout
- [ ] Legacy logging interface still works (via adapter)
- [ ] NullLogger fallback works for non-DI scenarios
- [ ] No sensitive data logged

### Phase 6 Success Criteria
- [ ] Serialization works with all existing payloads
- [ ] Nullable reference types enabled, no warnings
- [ ] XML documentation complete for public APIs
- [ ] Code analysis passes with no warnings
- [ ] Performance equivalent or better than before

## Testing Strategy

### Unit Testing
- Add xUnit test project
- Mock IHttpClientFactory for testing
- Test serialization/deserialization
- Test validation logic
- Test error handling

### Integration Testing
- Test against Riskified sandbox environment
- Verify HMAC authentication
- Test all API endpoints
- Validate async behavior
- Test timeout and retry scenarios

### Backward Compatibility Testing
- Ensure existing code patterns still work
- Test manual instantiation (non-DI)
- Verify synchronous methods (obsolete but functional)
- Test legacy logging via adapter

## Timeline Estimate

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: .NET 6+ | 1-2 days | None |
| Phase 2: HttpClient | 3-4 days | Phase 1 |
| Phase 3: Async/Await | 3-5 days | Phase 2 |
| Phase 4: DI | 2-3 days | Phase 3 |
| Phase 5: Logging | 2-3 days | Phase 4 |
| Phase 6: Serialization | 3-4 days | Phase 5 |
| **Total** | **14-21 days** | Sequential |

## Risk Mitigation

### High-Risk Areas
1. **HTTP Layer Rewrite (Phase 2)**: Complete rewrite of core infrastructure
   - Mitigation: Extensive testing, maintain feature parity

2. **Serialization Changes (Phase 6)**: Breaking changes in JSON handling
   - Mitigation: Comprehensive test coverage, consider keeping Newtonsoft.Json

### Backward Compatibility Guarantees
- All existing public APIs maintained
- Synchronous methods kept (marked obsolete with guidance)
- Legacy logging adapter provided
- Multi-targeting ensures .NET Framework compatibility
- New features are additive, not breaking

## Post-Modernization Recommendations

### Documentation
- Update README with modern usage examples
- Create migration guide for existing users
- Document DI patterns and configuration
- Add async/await best practices guide

### Testing
- Achieve >80% code coverage
- Add performance benchmarks
- Implement continuous integration
- Add mutation testing

### Future Enhancements
- Add retry policies with Polly
- Implement circuit breaker pattern
- Add telemetry with OpenTelemetry
- Consider gRPC support for high-performance scenarios
- Add rate limiting support

---

## Conclusion

This modernization effort will transform the Riskified SDK from a legacy .NET Framework 4.5.1 library into a modern, async-first, dependency-injection-ready SDK compatible with current .NET platforms. The phased approach ensures changes are reviewable, testable, and maintain backward compatibility throughout the process.

**Next Steps:** Begin Phase 1 by creating branch `jbarnett/1-update-net` and updating target frameworks.
