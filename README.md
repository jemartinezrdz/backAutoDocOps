# AutoDocOps Backend

Backend API robusta, rápida y segura para AutoDocOps que automatiza la generación de documentación a partir de código fuente.

## 🎯 Objetivo

Proveer una API que:
1. Reciba repositorios, ejecute el análisis IL/SQL y genere la documentación
2. Sirva endpoints REST/tRPC para proyectos, specs, chat y métricas
3. Escale horizontalmente con latencia p95 < 250 ms y coste infra ≤ 0.05 USD por pasaporte

## 🏗️ Arquitectura

### Clean Architecture
- **Domain**: Entidades de negocio y reglas de dominio
- **Application**: Casos de uso y lógica de aplicación (CQRS con MediatR)
- **Infrastructure**: Acceso a datos, servicios externos y persistencia
- **WebAPI**: Controladores, middleware y configuración de la API

### Stack Tecnológico
- **.NET 8** con AOT (Ahead of Time) compilation
- **C# 12** con nullable reference types habilitado
- **MediatR** para implementar patrón CQRS
- **Dapper** para queries optimizadas
- **EF Core** para migraciones
- **Supabase Postgres 14** con pgvector para embeddings
- **Redis (Upstash)** para cache y chat
- **gRPC** para comunicación con micro-servicio IL Scanner
- **OpenTelemetry** para observabilidad
- **Fly.io** para despliegue con autoscaling

## 🚀 Características

### Rendimiento
- Latencia p95 < 250 ms para endpoint /generate
- Cold-start < 100 ms con binario AOT
- Consumo RAM < 80 MB por instancia en idle

### Seguridad
- Autenticación JWT con Supabase Auth
- Row Level Security (RLS) en Postgres
- Rate limiting con Cloudflare WAF
- Gestión de secretos con Doppler

### Escalabilidad
- Autoscaling 0→N instancias en Fly.io
- API stateless para escalado horizontal
- PgBouncer para optimización de conexiones
- Sharding de Supabase para grandes volúmenes

## 📁 Estructura del Proyecto

```
backAutoDocOps/
├── src/
│   ├── AutoDocOps.Domain/          # Entidades y reglas de negocio
│   ├── AutoDocOps.Application/     # Casos de uso y CQRS
│   ├── AutoDocOps.Infrastructure/  # Persistencia y servicios externos
│   └── AutoDocOps.WebAPI/         # API endpoints y configuración
├── tests/
│   └── AutoDocOps.Tests/          # Tests unitarios y de integración
├── Dockerfile                     # Configuración de contenedor
└── AutoDocOps.sln                # Solución .NET
```

## 🛠️ Desarrollo

### Prerrequisitos
- .NET 8 SDK
- Docker
- PostgreSQL (o Supabase)
- Redis

### Comandos Básicos

```bash
# Restaurar dependencias
dotnet restore

# Compilar solución
dotnet build

# Ejecutar tests
dotnet test

# Ejecutar API en desarrollo
dotnet run --project src/AutoDocOps.WebAPI

# Construir imagen Docker
docker build -t autodocops-api .
```

### Variables de Entorno

```bash
ASPNETCORE_ENVIRONMENT=Development
DATABASE_URL=postgresql://...
REDIS_URL=redis://...
SUPABASE_URL=https://...
SUPABASE_ANON_KEY=...
OPENAI_API_KEY=...
```

## 📊 Métricas y Observabilidad

- **Logs estructurados** con Serilog → Grafana Loki
- **Métricas** con Prometheus → Grafana
- **Traces distribuidos** con OpenTelemetry → Tempo
- **Health checks** en `/health/live` y `/health/ready`
- **Métricas de aplicación** en `/metrics`

## 🔄 CI/CD

Pipeline automatizado con GitHub Actions:
1. Tests unitarios con cobertura ≥ 80%
2. Build de imagen Docker AOT
3. Deploy blue-green en Fly.io
4. Smoke tests post-deploy con Playwright

## 📈 Fases de Desarrollo

- [x] **Fase 0**: Scaffold & arquitectura base
- [ ] **Fase 1**: Entidades + casos de uso
- [ ] **Fase 2**: Parsers IL + SQL
- [ ] **Fase 3**: OpenAPI + tRPC Bridge
- [ ] **Fase 4**: Chat & embeddings
- [ ] **Fase 5**: Seguridad & RLS
- [ ] **Fase 6**: Observabilidad & métricas
- [ ] **Fase 7**: CI/CD & IaC
- [ ] **Fase 8**: Beta hardening

## 🎯 KPIs de Éxito

- Latencia p95 < 250 ms
- Coste infra/pasaporte ≤ 0.05 USD
- Churn API errors < 0.5%
- Cobertura tests backend ≥ 80%

## 📝 Licencia

Este proyecto está bajo licencia MIT.

