# Guía Inicial de Mejora de Tests

## Quick Wins Implementados
- Timeouts agregados a todos los tests unitarios (2-3s) para detectar colgados.
- Builders añadidos (`ProjectBuilder`, `PassportBuilder`) para reducir duplicación y preparar escenarios complejos.
- Configuración de cobertura (coverlet + ReportGenerator) en `AutoDocOps.Tests.csproj`.
 - Archivo de configuración `AutoDocOps.runsettings` para salida mínima y control de cobertura.

## Próximos Pasos Recomendados (Sprint Actual)
1. Añadir script Makefile/CI para generar reporte HTML:
   `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover`
   luego `reportgenerator -reports:tests/TestResults/coverage/coverage.opencover.xml -targetdir:tests/TestResults/report`
   (Ahora disponible via `make test-coverage`)
2. Introducir pruebas de integración mínimas para un endpoint de `Projects` usando `WebApplicationFactory`.
3. Agregar pruebas de validación de inputs maliciosos (SQLi/XSS path traversal) sobre comandos/queries relevantes.
4. Configurar ejecución de cobertura en pipeline y condición (>80%).

## Estándar de Nombres
`[Método|Handler]_[Escenario]_[ResultadoEsperado]`

## Timeout Default
Si un test requiere más de 2s, revisar:
- Uso accidental de I/O real
- Falta de mocks
- Esperas activas

## Builders
Usar en nuevos tests:
```csharp
var project = new ProjectBuilder().WithName("Nuevo").Build();
```

## Pendiente
- Mutation testing (Stryker)
- Contratos (Pact) para Stripe
- Benchmarks críticos
