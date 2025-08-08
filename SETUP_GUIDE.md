# 📋 Configuración Completa de AutoDocOps

## 🚀 Setup Rápido

### Para desarrolladores impacientes:

```bash
# En Windows PowerShell
.\scripts\dev-setup.ps1

# En Linux/Mac
chmod +x scripts/dev-setup.sh
./scripts/dev-setup.sh
```

## 📝 Setup Manual Detallado

### 1. Prerrequisitos

- ✅ .NET 8 SDK
- ✅ Docker Desktop
- ✅ Git
- ✅ Un editor de código (VS Code recomendado)

### 2. Clonar y Configurar

```bash
git clone https://github.com/jemartinezrdz/backAutoDocOps.git
cd backAutoDocOps

# Configurar variables de entorno
cp .env.example .env
# Editar .env con tus valores
```

### 3. Variables de Entorno Requeridas

```bash
# Database
DATABASE_CONNECTION_STRING=Host=localhost;Database=autodocops;Username=postgres;Password=postgres

# Redis
REDIS_CONNECTION_STRING=localhost:6379

# JWT
JWT_SECRET_KEY=your_secure_jwt_secret_key_32_chars_minimum

# OpenAI (opcional para desarrollo)
OPENAI_API_KEY=your_openai_api_key_here
OPENAI_API_BASE=your_azure_openai_endpoint_if_using_azure
USE_FAKE_LLM=true  # true para desarrollo sin API real

# Stripe (opcional para desarrollo)
STRIPE_SECRET_KEY=sk_test_your_stripe_key
STRIPE_WEBHOOK_SECRET=whsec_your_webhook_secret

# Environment
ASPNETCORE_ENVIRONMENT=Development
```

### 4. Construcción y Ejecución

```bash
# Usando Makefile (recomendado)
make setup-env     # Copia .env.example a .env
make build         # Construye la aplicación
make up-dev        # Inicia servicios en modo desarrollo

# O usando comandos directos
dotnet build src/AutoDocOps.WebAPI
docker compose -f docker-compose.yml -f docker-compose.override.yml up -d
```

## 🧪 Testing

### Tests Unitarios
```bash
make test
# o
dotnet test tests/AutoDocOps.Tests
```

### Tests Funcionales
```bash
make test-all   # Ejecuta todos los tests funcionales
# o
curl http://localhost:8080/api/test/health
curl http://localhost:8080/api/test/cache/demo
curl http://localhost:8080/api/test/system-info
```

## 🌐 Servicios Disponibles

| Servicio | URL | Credenciales |
|----------|-----|--------------|
| **API Principal** | http://localhost:8080 | - |
| **Swagger UI** | http://localhost:8080/swagger | - |
| **Health Checks** | http://localhost:8080/health | - |
| **pgAdmin** | http://localhost:5050 | admin@autodocops.com / admin |
| **Grafana** | http://localhost:3000 | admin / admin |
| **Prometheus** | http://localhost:9090 | - |

## 🔧 Comandos Útiles

```bash
# Ver logs de todos los servicios
make logs

# Ver logs de un servicio específico
make logs-webapi
make logs-postgres
make logs-redis

# Construir solo las imágenes Docker
make build-docker

# Parar todos los servicios
make down

# Limpiar todo (incluyendo volúmenes)
make clean

# Ejecutar en modo producción
make up-prod

# Ejecutar con monitoreo
make up-monitoring
```

## 🏥 Health Checks

Los health checks están disponibles en:

- **Básico**: `GET /health`
- **Liveness**: `GET /health/live`
- **Readiness**: `GET /health/ready`

Incluye verificaciones para:
- ✅ Base de datos PostgreSQL
- ✅ Cache Redis
- ✅ Servicio LLM (OpenAI/Fake)
- ✅ Servicio de documentación

## 📊 Monitoreo

### Grafana Dashboards
- API Latency Dashboard
- System Metrics
- Custom Dashboards en `monitoring/grafana/dashboards/`

### Prometheus Metrics
- Configuración en `monitoring/prometheus.yml`
- Métricas de la aplicación .NET
- Métricas de infraestructura

## 🚨 Troubleshooting

### Puerto ya en uso
```bash
# Verificar qué usa el puerto 8080
netstat -ano | findstr :8080  # Windows
lsof -i :8080                # Linux/Mac

# Parar servicios existentes
make down
```

### Base de datos no conecta
```bash
# Verificar estado de PostgreSQL
docker compose exec postgres pg_isready -U postgres

# Reiniciar solo PostgreSQL
docker compose restart postgres
```

### Cache Redis no funciona
```bash
# Verificar Redis
docker compose exec redis redis-cli ping

# Limpiar cache Redis
docker compose exec redis redis-cli FLUSHALL
```

### Logs detallados
```bash
# Ver logs con timestamps
docker compose logs -f --timestamps

# Ver solo errores
docker compose logs -f | grep ERROR
```

## 🔒 Seguridad

### Desarrollo
- ✅ Claves de prueba configuradas
- ✅ HTTPS opcional en desarrollo
- ✅ CORS permisivo para desarrollo

### Producción
- ⚠️ Cambiar todas las claves por valores seguros
- ⚠️ Habilitar HTTPS obligatorio
- ⚠️ Configurar CORS restrictivo
- ⚠️ Usar secretos de entorno, no archivos

## 📈 Próximos Pasos

1. **Implementar autenticación JWT completa**
2. **Agregar más tests de integración**
3. **Configurar CI/CD pipeline**
4. **Implementar rate limiting**
5. **Agregar más métricas de observabilidad**

## 💡 Tips de Desarrollo

- Usa `make dev-run` para desarrollo rápido sin Docker
- Los logs están en `logs/autodocops-*.log`
- Swagger UI incluye ejemplos para todos los endpoints
- Los health checks incluyen información detallada del sistema
- El TestController tiene endpoints para probar todas las integraciones
