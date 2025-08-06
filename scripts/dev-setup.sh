#!/bin/bash

# AutoDocOps Development Setup Script
# Este script configura el entorno de desarrollo automáticamente

set -e

# Configuración por defecto
API_BASE_URL=${API_BASE_URL:-"http://localhost:8080"}

echo "🚀 AutoDocOps Development Setup"
echo "================================"

# Verificar prerrequisitos
echo "📋 Verificando prerrequisitos..."

# Verificar .NET
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET 8 SDK no está instalado"
    echo "Instala .NET 8 SDK desde: https://dotnet.microsoft.com/download"
    exit 1
fi

# Verificar Docker
if ! command -v docker &> /dev/null; then
    echo "❌ Error: Docker no está instalado"
    echo "Instala Docker desde: https://www.docker.com/get-started"
    exit 1
fi

# Verificar Docker Compose
if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo "❌ Error: Docker Compose no está disponible"
    exit 1
fi

echo "✅ Prerrequisitos verificados"

# Configurar archivo de entorno
echo "🔧 Configurando archivo de entorno..."
if [ ! -f .env ]; then
    cp .env.example .env
    echo "✅ Archivo .env creado desde .env.example"
    echo "📝 Por favor, edita .env con tus configuraciones antes de continuar"
    echo "¿Quieres continuar con la configuración por defecto? (y/N)"
    read -r response
    if [[ ! "$response" =~ ^[Yy]$ ]]; then
        echo "⏸️  Setup pausado. Edita .env y ejecuta este script nuevamente."
        exit 0
    fi
else
    echo "✅ Archivo .env ya existe"
fi

# Crear directorios necesarios
echo "📁 Creando directorios necesarios..."
mkdir -p logs
mkdir -p data/postgres
mkdir -p data/redis

# Construir aplicación
echo "🔨 Construyendo aplicación .NET..."
dotnet build src/AutoDocOps.WebAPI --configuration Debug

# Ejecutar tests
echo "🧪 Ejecutando tests unitarios..."
dotnet test tests/AutoDocOps.Tests --logger "console;verbosity=minimal"

# Construir imágenes Docker
echo "🐳 Construyendo imágenes Docker..."
docker compose build

# Iniciar servicios
echo "🚀 Iniciando servicios..."
docker compose -f docker-compose.yml -f docker-compose.override.yml up -d

# Esperar a que los servicios estén listos
echo "⏳ Esperando a que los servicios estén listos..."
sleep 10

# Verificar estado de los servicios
echo "🔍 Verificando estado de los servicios..."

# Verificar PostgreSQL
if docker compose exec postgres pg_isready -U postgres > /dev/null 2>&1; then
    echo "✅ PostgreSQL está listo"
else
    echo "❌ PostgreSQL no está listo"
fi

# Verificar Redis
if docker compose exec redis redis-cli ping > /dev/null 2>&1; then
    echo "✅ Redis está listo"
else
    echo "❌ Redis no está listo"
fi

# Verificar WebAPI
if curl -f "${API_BASE_URL}/health/live" > /dev/null 2>&1; then
    echo "✅ WebAPI está listo"
else
    echo "❌ WebAPI no está listo"
fi

# Mostrar información de servicios
echo ""
echo "🎉 Setup completado!"
echo "==================="
echo ""
echo "🌐 Servicios disponibles:"
echo "  • API: ${API_BASE_URL}"
echo "  • Swagger: ${API_BASE_URL}/swagger"
echo "  • pgAdmin: http://localhost:5050 (admin@autodocops.com / admin)"
echo ""
echo "🔧 Comandos útiles:"
echo "  • Ver logs: docker compose logs -f"
echo "  • Parar servicios: docker compose down"
echo "  • Reiniciar: docker compose restart"
echo ""
echo "🧪 Endpoints de prueba:"
echo "  • Health: curl ${API_BASE_URL}/api/test/health"
echo "  • Cache: curl ${API_BASE_URL}/api/test/cache/demo"
echo "  • System Info: curl ${API_BASE_URL}/api/test/system-info"
echo ""
echo "📝 Para ejecutar tests funcionales:"
echo "  make test-all"
