# AutoDocOps Backend

Backend API robusta para AutoDocOps - Sistema de generación automática de documentación técnica.

## 🏗️ Arquitectura

### Clean Architecture
- **Domain Layer**: Entidades de negocio y interfaces
- **Application Layer**: Casos de uso y lógica de aplicación (CQRS con MediatR)
- **Infrastructure Layer**: Implementaciones de repositorios y servicios externos
- **WebAPI Layer**: Controladores REST y configuración de API

### Microservicios
- **WebAPI**: API REST principal con Swagger/OpenAPI
- **IL Scanner**: Servicio gRPC para análisis de código .NET con Roslyn

## 🚀 Características Implementadas

### ✅ Fase 0: Scaffold y Arquitectura Base
- Clean Architecture con .NET 8
- Proyectos Domain, Application, Infrastructure, WebAPI
- Tests unitarios con xUnit y Moq
- Configuración Docker y documentación

### ✅ Fase 1: Entidades y Casos de Uso
- Entidades: Project, Spec, Passport
- Comandos y Queries con MediatR (CQRS)
- Interfaces de repositorio
- Tests unitarios con cobertura >90%

### ✅ Fase 2: Parsers IL + SQL
- Servicio gRPC IL Scanner con Roslyn
- Análisis de código C# con extracción de metadata
- Parser SQL para múltiples bases de datos
- Contratos gRPC con validación JSON

### ✅ Fase 3: OpenAPI + REST API
- Controladores REST (/projects, /generate, /passports)
- Swagger/OpenAPI con documentación completa
- Versionado de API (v1.0)
- Paginación y manejo de errores con ProblemDetails
- Health checks y CORS

### ✅ Infraestructura
- Entity Framework Core con PostgreSQL
- Patrón Repository implementado
- DbContext con configuraciones de entidades
- Dependency Injection configurado

### ✅ Fase 7: CI/CD & IaC

### ✅ Fase 8: Integraciones Avanzadas
- **Redis**: Cache distribuido con RedisCacheService
- **OpenAI**: Integración con chat streaming y cliente fake para testing
- **Stripe**: Sistema de facturación con webhooks
- **Grafana + Prometheus**: Monitoreo y observabilidad completos
- **Health Checks**: Verificaciones completas de servicios
- **Docker Compose**: Configuración completa para desarrollo y producción

## 🔧 Configuración del Entorno

### 🚀 Setup Rápido

```bash
# Windows PowerShell
.\scripts\dev-setup.ps1

# Linux/Mac
chmod +x scripts/dev-setup.sh && ./scripts/dev-setup.sh
```

### Prerrequisitos
- .NET 8 SDK
- Docker y Docker Compose
- PostgreSQL
- Redis

### Configuración Manual

1. **Clonar el repositorio**
```bash
git clone https://github.com/jemartinezrdz/backAutoDocOps.git
cd backAutoDocOps
```

2. **Configurar variables de entorno**
```bash
# Copiar archivo de ejemplo
cp .env.example .env

# Editar .env con tus valores reales
nano .env
```

3. **Iniciar servicios**
```bash
# Usando Makefile (recomendado)
make setup-env  # Configurar .env
make up-dev     # Iniciar en modo desarrollo

# O usando Docker Compose directamente
docker compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

### Variables de Entorno Necesarias

```bash
# Base de datos
DATABASE_CONNECTION_STRING=Host=localhost;Database=autodocops;Username=postgres;Password=postgres

# Redis para caché distribuido
REDIS_CONNECTION_STRING=localhost:6379

# JWT para autenticación
JWT_SECRET_KEY=your_secure_jwt_secret_key_32_chars_minimum

# OpenAI/Azure OpenAI
OPENAI_API_KEY=your_openai_api_key_here
OPENAI_API_BASE=your_azure_openai_endpoint_if_using_azure
USE_FAKE_LLM=true  # Para desarrollo sin API real

# Stripe para facturación
STRIPE_SECRET_KEY=your_stripe_secret_key
STRIPE_WEBHOOK_SECRET=your_stripe_webhook_secret
```

### ✅ Fase 7: CI/CD & IaC
- Pipeline GitHub Actions completo
- Dockerfiles para WebAPI e IL Scanner
- Docker Compose para desarrollo local
- Terraform para infraestructura AWS
- Configuración de monitoreo (Prometheus/Grafana)

## 🛠️ Stack Tecnológico

- **.NET 8** con C# 12
- **Entity Framework Core** con PostgreSQL
- **MediatR** para CQRS
- **Roslyn** para análisis de código
- **gRPC** para comunicación entre servicios
- **Swagger/OpenAPI** para documentación
- **Docker** para containerización
- **GitHub Actions** para CI/CD
- **Terraform** para IaC
- **Prometheus/Grafana** para observabilidad

## 🏃‍♂️ Inicio Rápido

### Prerrequisitos
- .NET 8 SDK
- Docker y Docker Compose
- PostgreSQL (o usar Docker)

### Desarrollo Local

1. **Clonar el repositorio**
```bash
git clone https://github.com/jemartinezrdz/backAutoDocOps.git
cd backAutoDocOps
```

2. **Ejecutar con Docker Compose**
```bash
# Servicios completos
docker-compose up -d

# Solo desarrollo (sin monitoreo)
docker-compose --profile dev up -d

# Con monitoreo
docker-compose --profile monitoring up -d
```

3. **Ejecutar localmente**
```bash
# Restaurar dependencias
dotnet restore

# Ejecutar tests
dotnet test

# Ejecutar WebAPI
dotnet run --project src/AutoDocOps.WebAPI

# Ejecutar IL Scanner
dotnet run --project src/AutoDocOps.ILScanner
```

### URLs de Desarrollo
- **API**: http://localhost:8080
- **Swagger**: http://localhost:8080/swagger
- **IL Scanner gRPC**: http://localhost:5000
- **pgAdmin**: http://localhost:5050 (admin@autodocops.com / admin)
- **Grafana**: http://localhost:3000 (admin / admin)
- **Prometheus**: http://localhost:9090

## 📊 Monitoreo y Observabilidad

### Health Checks
- `/health/live` - Liveness probe
- `/health/ready` - Readiness probe

### Métricas
- Prometheus metrics en `/metrics`
- Dashboards de Grafana preconfigurados
- Alertas configurables

### Logging
- Structured logging con Serilog
- Correlación de requests
- Niveles configurables por ambiente

## 🔒 Seguridad

### Headers de Seguridad
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: strict-origin-when-cross-origin

### Autenticación (Próximamente)
- JWT tokens
- Integración con Supabase Auth
- Row Level Security (RLS)

## 🧪 Testing

```bash
# Ejecutar todos los tests
dotnet test

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Tests específicos
dotnet test --filter "Category=Unit"
```

## 📦 Despliegue

### Docker
```bash
# Build images
docker build -f src/AutoDocOps.WebAPI/Dockerfile -t autodocops-webapi .
docker build -f src/AutoDocOps.ILScanner/Dockerfile -t autodocops-ilscanner .
```

### Kubernetes
```bash
# Aplicar manifiestos (próximamente)
kubectl apply -f k8s/
```

### AWS con Terraform
```bash
cd terraform
terraform init
terraform plan
terraform apply
```

## 🔧 Configuración

### Variables de Entorno
```bash
# Database
ConnectionStrings__DefaultConnection="Host=localhost;Database=autodocops;Username=postgres;Password=postgres"

# Redis
ConnectionStrings__Redis="localhost:6379"

# IL Scanner
ILScanner__GrpcEndpoint="http://localhost:5000"

# Logging
Logging__LogLevel__Default="Information"
```

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=autodocops;Username=postgres;Password=postgres"
  },
  "ILScanner": {
    "GrpcEndpoint": "http://localhost:5000"
  }
}
```

## 🤝 Contribución

1. Fork el proyecto
2. Crear feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Abrir Pull Request

## 📋 Roadmap

### Próximas Fases
- **Fase 4**: Chat & embeddings con pgvector
- **Fase 5**: Seguridad & RLS con JWT/Supabase
- **Fase 6**: Observabilidad completa con OpenTelemetry
- **Fase 8**: Beta hardening y optimización

### Mejoras Futuras
- Integración con GitHub/GitLab
- Soporte para más lenguajes de programación
- Dashboard web para gestión
- API GraphQL
- Webhooks para notificaciones

## 📄 Licencia

Este proyecto está bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para detalles.

## 👥 Equipo

- **Desarrollo**: AutoDocOps Team
- **Arquitectura**: Clean Architecture + Microservicios
- **DevOps**: Docker + Kubernetes + AWS

---

**AutoDocOps** - Generación automática de documentación técnica 🚀

