# User Secrets Setup - Quick Start

## What Are User Secrets?

User Secrets is a .NET feature that stores sensitive configuration data **outside your repository**, in your user profile directory. This is the **most secure** way to store credentials locally.

## Location

Secrets are stored in:
- **macOS/Linux:** `~/.microsoft/usersecrets/riskified-sdk-tests-a1b2c3d4/secrets.json`
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\riskified-sdk-tests-a1b2c3d4\secrets.json`

This directory is **never** added to git - it's completely outside your project.

## Setup Your Riskified Credentials

### Step 1: Navigate to Test Project
```bash
cd Riskified.SDK.Tests
```

### Step 2: Set Your Credentials
```bash
# Set your Riskified Sandbox domain
dotnet user-secrets set "Riskified:MerchantDomain" "your-shop.myshopify.com"

# Set your authentication token
dotnet user-secrets set "Riskified:MerchantAuthenticationToken" "your-actual-token-here"

# Set environment (usually Sandbox for testing)
dotnet user-secrets set "Riskified:RiskifiedEnvironment" "Sandbox"
```

### Step 3: Verify Your Secrets
```bash
# List all secrets
dotnet user-secrets list

# Should show:
# Riskified:MerchantDomain = your-shop.myshopify.com
# Riskified:MerchantAuthenticationToken = your-actual-token-here
# Riskified:RiskifiedEnvironment = Sandbox
```

### Step 4: Run Tests
```bash
cd ..
dotnet test Riskified.SDK.Tests
```

The tests will automatically load your secrets! ✨

## Managing Secrets

### View All Secrets
```bash
dotnet user-secrets list
```

### Update a Secret
```bash
dotnet user-secrets set "Riskified:MerchantAuthenticationToken" "new-token-here"
```

### Remove a Secret
```bash
dotnet user-secrets remove "Riskified:MerchantAuthenticationToken"
```

### Clear All Secrets
```bash
dotnet user-secrets clear
```

## Why User Secrets?

✅ **Secure** - Stored outside your repository
✅ **Per-Developer** - Each team member has their own credentials
✅ **No Git Risk** - Impossible to accidentally commit secrets
✅ **Easy** - Simple command-line interface
✅ **Standard** - Built into .NET, used by Microsoft and industry

## Alternative: Environment Variables (CI/CD)

For automated builds (GitHub Actions, Azure DevOps, etc.):

```yaml
# GitHub Actions example
env:
  Riskified__MerchantDomain: ${{ secrets.RISKIFIED_DOMAIN }}
  Riskified__MerchantAuthenticationToken: ${{ secrets.RISKIFIED_TOKEN }}
  Riskified__RiskifiedEnvironment: "Sandbox"
```

Note the double underscore `__` for environment variables (replaces `:` in JSON paths).

## Common Issues

### "Missing Riskified:MerchantDomain"
You haven't set your secrets yet. Run the Step 2 commands above.

### Secrets Not Loading
Make sure you're in the test project directory when setting secrets:
```bash
cd Riskified.SDK.Tests
dotnet user-secrets set "Riskified:MerchantDomain" "your-domain"
```

### Want to See the File Directly?
```bash
# macOS/Linux
cat ~/.microsoft/usersecrets/riskified-sdk-tests-a1b2c3d4/secrets.json

# Windows
type %APPDATA%\Microsoft\UserSecrets\riskified-sdk-tests-a1b2c3d4\secrets.json
```