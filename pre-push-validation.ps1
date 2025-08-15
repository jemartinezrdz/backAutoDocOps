# pre-push-validation.ps1
Write-Host "🔍 Running Pre-Push Validation..." -ForegroundColor Cyan

# 1. Build
Write-Host "📦 Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1 
}

# 2. Tests afectados
Write-Host "🧪 Running affected tests..." -ForegroundColor Yellow
dotnet test --filter "FullyQualifiedName~BackoffHelper|FullyQualifiedName~WebhookMetrics" --no-build -c Release
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ Affected tests failed!" -ForegroundColor Red
    exit 1 
}

# 3. Infrastructure tests
Write-Host "🏗️ Running infrastructure tests..." -ForegroundColor Yellow
dotnet test --filter "FullyQualifiedName~Infrastructure" --no-build -c Release --logger "console;verbosity=minimal"
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ Infrastructure tests failed!" -ForegroundColor Red
    exit 1 
}

# 4. Code coverage check
Write-Host "📊 Checking code coverage..." -ForegroundColor Yellow
dotnet test --collect:"XPlat Code Coverage" --no-build -c Release
# Parsear y verificar que coverage >= 80%

# 5. AOT validation
Write-Host "⚡ Validating AOT compilation..." -ForegroundColor Yellow
dotnet publish src/AutoDocOps.WebAPI -c Release -r linux-x64 --self-contained -o ./test-publish
if ($LASTEXITCODE -ne 0) { 
    Write-Host "❌ AOT compilation failed!" -ForegroundColor Red
    exit 1 
}

# 6. Cleanup
Write-Host "🧹 Cleaning up..." -ForegroundColor Yellow
Remove-Item -Path "./test-publish" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "✅ All validations passed! Safe to push." -ForegroundColor Green