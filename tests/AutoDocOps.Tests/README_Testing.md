# Guía de Pruebas del Proyecto AutoDocOps

Este documento establece las directrices, estándares y herramientas para la escritura de pruebas en el proyecto, con el objetivo de asegurar la calidad, robustez y mantenibilidad del código.

## Filosofía de Testing
Creemos que una estrategia de pruebas sólida es fundamental para el desarrollo ágil y la entrega continua. Las pruebas no solo previenen regresiones, sino que también sirven como documentación viva del comportamiento del sistema.

## Configuración Actual
- **Motor de Pruebas**: Las pruebas se ejecutan con `xUnit`.
- **Cobertura de Código**: Se mide con `Coverlet` y los reportes HTML se generan con `ReportGenerator`.
- **Configuración de Ejecución**: El archivo `AutoDocOps.runsettings` está configurado para controlar la salida y los parámetros de cobertura.
- **Script de CI**: El comando `make test-coverage` genera un reporte de cobertura completo en `tests/TestResults/report`.

---

## Tipos de Pruebas

### 1. Pruebas Unitarias (Unit Tests)
- **Propósito**: Verificar una única unidad de código (una clase, un método) de forma completamente aislada.
- **Ubicación**: `tests/AutoDocOps.Tests/`.
- **Reglas**:
    - **NO** deben realizar operaciones de I/O (red, sistema de archivos, base de datos).
    - Todas las dependencias externas deben ser simuladas (mockeadas) usando `Moq`.

### 2. Pruebas de Integración (Integration Tests)
- **Propósito**: Verificar la correcta colaboración entre múltiples componentes del sistema, incluyendo la base de datos, el contenedor de DI y la configuración.
- **Ubicación**: (Recomendado) Crear un nuevo proyecto `tests/AutoDocOps.IntegrationTests/`.
- **Herramientas**: Se debe usar `WebApplicationFactory` para levantar una versión en memoria de la API y una base de datos de prueba (ej. en un contenedor de Docker o en memoria).

### 3. Pruebas de Seguridad (Security Tests)
- **Propósito**: Validar que la aplicación es resistente a ataques comunes y que las políticas de autorización son correctas.
- **Ejemplos**:
    - Intentos de inyección de SQL/XSS en los parámetros de entrada.
    - Verificación de que un usuario no puede acceder a recursos de otra organización.
    - Comprobación de que los endpoints protegidos requieren un token válido.

---

## Estándares y Convenciones

### Estructura de Archivos
Las carpetas en el proyecto de pruebas deben replicar la estructura del proyecto de código fuente (`src`).

### Nomenclatura de Pruebas
Se utiliza el formato `[Método|Handler]_[Escenario]_[ResultadoEsperado]`.
**Ejemplo**: `CreateProjectHandler_WithValidInput_ReturnsSuccessResponse`

### Timeouts por Defecto
- Un test no debería tardar más de **2 segundos**.
- Si un test excede este límite, es un indicio de que puede estar realizando I/O real, le faltan mocks o contiene esperas ineficientes.
- **Recomendación**: Definir constantes compartidas para los timeouts (ej. `public const int DefaultTimeoutMs = 2000;`) para mantener la consistencia.

---

## Patrones y Herramientas

### Librerías Principales
- **`xUnit`**: El framework para definir y ejecutar las pruebas.
- **`Moq`**: La librería para crear mocks y simular dependencias.

### Estrategia de Mocks
- **Qué mockear**: Siempre se deben mockear las dependencias que cruzan los límites del sistema (I/O), como repositorios (`IProjectRepository`), servicios de terceros (`IBillingService`, `ILlmClient`) o el sistema de archivos.
- **Cómo mockear**: Los mocks deben ser simples. Si la configuración de un mock se vuelve muy compleja, puede ser una señal para revisar el diseño de la clase que se está probando.

### Builders para Datos de Prueba
Se utiliza el patrón Builder para crear entidades de dominio de forma limpia y legible.
```csharp
// Uso del Builder
var project = new ProjectBuilder()
    .WithName("Proyecto de Prueba")
    .WithOrganizationId(Guid.NewGuid())
    .Build();
```

### Aserciones (Assertions)
- **Recomendación**: Se sugiere adoptar `FluentAssertions` para escribir aserciones más legibles y expresivas.
- **Ejemplo con FluentAssertions**: `result.Should().Be(expectedResult);`
- **Ejemplo con xUnit**: `Assert.Equal(expectedResult, result);`

---

## Hoja de Ruta de Mejoras

### Alta Prioridad
- **(Hacer) Pruebas de Integración**: Añadir pruebas de integración para el flujo de creación y consulta de Proyectos usando `WebApplicationFactory`.
- **(Hacer) Pruebas de Seguridad**: Implementar validaciones contra inputs maliciosos (SQLi, XSS, Path Traversal) en los handlers de comandos.
- **(Hacer) Umbral de Cobertura en CI**: Configurar el pipeline de CI para que falle si la cobertura de pruebas baja del 80%.

### Mediana Prioridad
- **(Explorar) Mutation Testing**: Usar `Stryker` para evaluar la calidad real de las pruebas. Una buena prueba debe fallar si se introduce un bug (una mutación) en el código que cubre.
- **(Explorar) Pruebas de Contrato**: Usar `Pact` para asegurar que la integración con la API de Stripe no se rompa cuando esta cambie, sin necesidad de realizar llamadas reales en el CI.

### Baja Prioridad
- **(Explorar) Benchmarks**: Implementar benchmarks con `BenchmarkDotNet` para medir y prevenir regresiones de rendimiento en puntos críticos, como los endpoints de generación.