# AutoDocOps Development Setup Script for Windows
# Este script configura el entorno de desarrollo automáticamente

param(
    [switch]$SkipTests = $false,
    [switch]$Force = $false
)

Write-Host "🚀 AutoDocOps Development Setup" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green

# Verificar prerrequisitos
Write-Host "📋 Verificando prerrequisitos..." -ForegroundColor Yellow

# Verificar .NET
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK encontrado: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: .NET 8 SDK no está instalado" -ForegroundColor Red
    Write-Host "Instala .NET 8 SDK desde: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Verificar Docker
try {
    $dockerVersion = docker --version
    Write-Host "✅ Docker encontrado: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: Docker no está instalado" -ForegroundColor Red
    Write-Host "Instala Docker Desktop desde: https://www.docker.com/get-started" -ForegroundColor Yellow
    exit 1
}

# Verificar Docker Compose
try {
    docker compose version | Out-Null
    Write-Host "✅ Docker Compose disponible" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: Docker Compose no está disponible" -ForegroundColor Red
    exit 1
}

# Configurar archivo de entorno
Write-Host "🔧 Configurando archivo de entorno..." -ForegroundColor Yellow
if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "✅ Archivo .env creado desde .env.example" -ForegroundColor Green
    Write-Host "📝 Por favor, edita .env con tus configuraciones antes de continuar" -ForegroundColor Yellow
    
    if (-not $Force) {
        $response = Read-Host "¿Quieres continuar con la configuración por defecto? (y/N)"
        if ($response -notmatch "^[Yy]$") {
            Write-Host "⏸️  Setup pausado. Edita .env y ejecuta este script nuevamente." -ForegroundColor Yellow
            exit 0
        }
    }
} else {
    Write-Host "✅ Archivo .env ya existe" -ForegroundColor Green
}

# Crear directorios necesarios
Write-Host "📁 Creando directorios necesarios..." -ForegroundColor Yellow
$directories = @("logs", "data\postgres", "data\redis")
foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "✅ Directorio creado: $dir" -ForegroundColor Green
    }
}

# Construir aplicación
Write-Host "🔨 Construyendo aplicación .NET..." -ForegroundColor Yellow
try {
    dotnet build src\AutoDocOps.WebAPI --configuration Debug
    Write-Host "✅ Aplicación construida exitosamente" -ForegroundColor Green
} catch {
    Write-Host "❌ Error construyendo la aplicación" -ForegroundColor Red
    exit 1
}

# Ejecutar tests (opcional)
if (-not $SkipTests) {
    Write-Host "🧪 Ejecutando tests unitarios..." -ForegroundColor Yellow
    try {
        dotnet test tests\AutoDocOps.Tests --logger "console;verbosity=minimal"
        Write-Host "✅ Tests ejecutados exitosamente" -ForegroundColor Green
    } catch {
        Write-Host "⚠️  Algunos tests fallaron, pero continuando..." -ForegroundColor Yellow
    }
}

# Construir imágenes Docker
Write-Host "🐳 Construyendo imágenes Docker..." -ForegroundColor Yellow
try {
    docker compose build
    Write-Host "✅ Imágenes Docker construidas" -ForegroundColor Green
} catch {
    Write-Host "❌ Error construyendo imágenes Docker" -ForegroundColor Red
    exit 1
}

# Iniciar servicios
Write-Host "🚀 Iniciando servicios..." -ForegroundColor Yellow
try {
    docker compose -f docker-compose.yml -f docker-compose.override.yml up -d
    Write-Host "✅ Servicios iniciados" -ForegroundColor Green
} catch {
    Write-Host "❌ Error iniciando servicios" -ForegroundColor Red
    exit 1
}

# Esperar a que los servicios estén listos
Write-Host "⏳ Esperando a que los servicios estén listos..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Verificar estado de los servicios
Write-Host "🔍 Verificando estado de los servicios..." -ForegroundColor Yellow

# Verificar WebAPI
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8080/health/live" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ WebAPI está listo" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ WebAPI no está listo" -ForegroundColor Red
}

# Mostrar información de servicios
Write-Host ""
Write-Host "🎉 Setup completado!" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
Write-Host ""
Write-Host "🌐 Servicios disponibles:" -ForegroundColor Cyan
Write-Host "  • API: http://localhost:8080" -ForegroundColor White
Write-Host "  • Swagger: http://localhost:8080/swagger" -ForegroundColor White
Write-Host "  • pgAdmin: http://localhost:5050 (admin@autodocops.com / admin)" -ForegroundColor White
Write-Host ""
Write-Host "🔧 Comandos útiles:" -ForegroundColor Cyan
Write-Host "  • Ver logs: docker compose logs -f" -ForegroundColor White
Write-Host "  • Parar servicios: docker compose down" -ForegroundColor White
Write-Host "  • Reiniciar: docker compose restart" -ForegroundColor White
Write-Host ""
Write-Host "🧪 Endpoints de prueba:" -ForegroundColor Cyan
Write-Host "  • Health: curl http://localhost:8080/api/test/health" -ForegroundColor White
Write-Host "  • Cache: curl http://localhost:8080/api/test/cache/demo" -ForegroundColor White
Write-Host "  • System Info: curl http://localhost:8080/api/test/system-info" -ForegroundColor White
Write-Host ""
Write-Host "📝 Para ejecutar tests funcionales en PowerShell:" -ForegroundColor Cyan
Write-Host "  Invoke-WebRequest -Uri 'http://localhost:8080/api/test/health' | ConvertFrom-Json" -ForegroundColor White
