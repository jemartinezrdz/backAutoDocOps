#!/bin/bash
# pre-push-validation.sh

echo "🔍 Running Pre-Push Validation..."

# 1. Build
echo "📦 Building solution..."
dotnet build --configuration Release
if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

# 2. Tests afectados
echo "🧪 Running affected tests..."
dotnet test --filter "FullyQualifiedName~BackoffHelper|FullyQualifiedName~WebhookMetrics" --no-build -c Release
if [ $? -ne 0 ]; then
    echo "❌ Affected tests failed!"
    exit 1
fi

# 3. Infrastructure tests
echo "🏗️ Running infrastructure tests..."
dotnet test --filter "FullyQualifiedName~Infrastructure" --no-build -c Release --logger "console;verbosity=minimal"
if [ $? -ne 0 ]; then
    echo "❌ Infrastructure tests failed!"
    exit 1
fi

# 4. Code coverage check
echo "📊 Checking code coverage..."
dotnet test --collect:"XPlat Code Coverage" --no-build -c Release

# 5. AOT validation
echo "⚡ Validating AOT compilation..."
dotnet publish src/AutoDocOps.WebAPI -c Release -r linux-x64 --self-contained -o ./test-publish
if [ $? -ne 0 ]; then
    echo "❌ AOT compilation failed!"
    exit 1
fi

# 6. Cleanup
echo "🧹 Cleaning up..."
rm -rf ./test-publish

echo "✅ All validations passed! Safe to push."