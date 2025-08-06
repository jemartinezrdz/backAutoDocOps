#!/bin/bash
# AutoDocOps Critical Security Fixes Script
# Execute before commit to ensure production readiness

set -e  # Exit on any error

echo "🔧 Executing AutoDocOps Critical Security Fixes..."

# 1. Validate no hardcoded secrets in versioned files
echo "🔍 Checking for hardcoded secrets..."
if grep -r "password=postgres\|sk_test_\|pk_test_\|sk_live_\|pk_live_" --include="*.cs" --include="*.json" --exclude-dir=".git" .; then
    echo "❌ ERROR: Hardcoded secrets found in versioned files!"
    echo "   Please remove all hardcoded secrets and use environment variables."
    exit 1
fi

# 2. Ensure .env.dev is not committed (if it exists)
if [ -f ".env.dev" ]; then
    if git ls-files --error-unmatch .env.dev 2>/dev/null; then
        echo "❌ ERROR: .env.dev is tracked by Git!"
        echo "   Run: git rm --cached .env.dev && echo '.env.dev' >> .gitignore"
        exit 1
    fi
fi

# 3. Validate JWT secret length in example
echo "🔑 Validating JWT configuration examples..."
if grep -q "REPLACE_WITH_SECURE_JWT_SECRET_KEY_32_CHARS_MINIMUM" .env.example; then
    echo "✅ JWT secret placeholder is properly configured"
else
    echo "❌ ERROR: JWT secret placeholder not found in .env.example"
    exit 1
fi

# 4. Check for hardcoded connection strings in DI
echo "🔗 Checking dependency injection configuration..."
if grep -q "Host=localhost.*Password=postgres" src/AutoDocOps.Infrastructure/DependencyInjection.cs; then
    echo "❌ ERROR: Hardcoded connection string found in DependencyInjection.cs"
    echo "   This was supposed to be fixed - please verify the changes."
    exit 1
fi

# 5. Validate required configuration models exist
echo "📋 Validating configuration models..."
if [ ! -f "src/AutoDocOps.Application/Common/Models/DbSettings.cs" ]; then
    echo "❌ ERROR: DbSettings.cs configuration model is missing"
    exit 1
fi

if [ ! -f "tests/AutoDocOps.Tests/Configuration/MissingEnvTests.cs" ]; then
    echo "❌ ERROR: Environment validation tests are missing"
    exit 1
fi

# 6. Run configuration validation tests
echo "🧪 Running environment validation tests..."
cd tests/AutoDocOps.Tests
if ! dotnet test --filter "MissingEnvTests" --verbosity quiet; then
    echo "❌ ERROR: Environment validation tests failed"
    echo "   Please fix configuration issues before proceeding."
    exit 1
fi
cd ../..

# 7. Build project to ensure no compilation errors
echo "🏗️ Building project to verify compilation..."
if ! dotnet build --configuration Release --verbosity quiet; then
    echo "❌ ERROR: Project compilation failed"
    echo "   Please fix compilation errors before proceeding."
    exit 1
fi

echo "✅ All critical security fixes validated successfully!"
echo ""
echo "🔐 Security Checklist Complete:"
echo "   ✅ No hardcoded secrets in versioned files"
echo "   ✅ .env.dev properly excluded from Git"
echo "   ✅ JWT configuration validated"
echo "   ✅ Connection strings use proper environment variables"
echo "   ✅ Configuration models with validation exist"
echo "   ✅ Environment validation tests pass"
echo "   ✅ Project compiles successfully"
echo ""
echo "🚀 Ready for commit and deployment!"
