# Code Standards Tests

Este directorio contiene tests automatizados que previenen la regresión de los problemas de calidad de código que se han solucionado.

## Tests Incluidos

### `CodeStandardsTests`

Esta clase de tests verifica que se mantengan los estándares de código y previene que vuelvan a ocurrir los warnings que se han solucionado:

#### 1. `ShouldNotUseDirectLoggerCalls_CA1848_Prevention()`
- **Propósito**: Previene el uso directo de llamadas de logging (`_logger.LogInformation()`) 
- **Solución**: Debe usar `LoggerMessage` delegates para mejor performance
- **Baseline actual**: ~59 violaciones permitidas para código existente
- **Archivos exceptuados**: Tests, ILScanner, SqlAnalyzer, RoslynAnalyzer

#### 2. `ShouldHaveArgumentNullValidation_CA1062_Prevention()`
- **Propósito**: Verifica que controllers con métodos complejos tengan validación de argumentos nulos
- **Solución**: Usar `ArgumentNullException.ThrowIfNull()` para validar parámetros
- **Baseline actual**: ≤3 violaciones permitidas
- **Enfoque**: Controllers con métodos POST/PUT que reciben objetos complejos

#### 3. `ShouldUseInvariantCulture_CA1304_CA1311_Prevention()`
- **Propósito**: Previene operaciones dependientes de cultura
- **Patrones detectados**:
  - `ToLower()` → debe usar `ToLowerInvariant()`
  - `ToUpper()` → debe usar `ToUpperInvariant()`
  - `StartsWith()` sin `StringComparison` → debe especificar `StringComparison.OrdinalIgnoreCase`
  - `EndsWith()` sin `StringComparison` → debe especificar comparación
  - `Contains()` sin `StringComparison` → debe especificar comparación

#### 4. `ShouldUseConfigureAwaitFalse_CA2007_Prevention()`
- **Propósito**: Previene deadlocks en código de librería
- **Solución**: Usar `ConfigureAwait(false)` en código de infraestructura/servicios
- **Baseline actual**: <100 violaciones permitidas
- **Archivos exceptuados**: Tests, Controllers, Program.cs (ASP.NET Core no necesita ConfigureAwait(false))

#### 5. `ShouldHaveLoggerMessageDefinitions()`
- **Propósito**: Verifica que existe la clase de LoggerMessage y tiene suficientes definiciones
- **Requisitos**:
  - Debe existir clase `static partial class Log` o `Logging`
  - Debe tener al menos 10 definiciones de `[LoggerMessage]`

#### 6. `ShouldNotHaveHardcodedStrings_InLogging()`
- **Propósito**: Previene strings hardcodeados en logging
- **Solución**: Usar `LoggerMessage` delegates en lugar de strings directos
- **Baseline actual**: <40 violaciones permitidas
- **Archivos exceptuados**: Tests, ILScanner, SqlAnalyzer, RoslynAnalyzer

## Uso

### Ejecutar todos los tests de calidad de código:
```bash
dotnet test --filter "FullyQualifiedName~CodeStandardsTests"
```

### Ejecutar un test específico:
```bash
dotnet test --filter "ShouldNotUseDirectLoggerCalls_CA1848_Prevention"
```

## Configuración de Baselines

Los tests están configurados con baselines permisivos para código existente pero previenen que el problema empeore. Los umbrales están basados en el estado actual del código:

- **CA1848 (LoggerMessage)**: <70 violaciones (actual: ~59)
- **CA1062 (ArgumentNull)**: ≤3 violaciones
- **CA1304/CA1311 (Culture)**: 0 violaciones (muy estricto)
- **CA2007 (ConfigureAwait)**: <100 violaciones
- **Hardcoded Logging**: <40 violaciones (actual: ~35)

## Mejorando el Código

Para reducir las violaciones existentes:

1. **Para CA1848**: Migrar llamadas directas de logging a usar la clase `Log` con `LoggerMessage`
2. **Para CA1062**: Añadir validación `ArgumentNullException.ThrowIfNull()` en métodos públicos
3. **Para CA1304/CA1311**: Usar métodos invariantes y especificar `StringComparison`
4. **Para CA2007**: Añadir `ConfigureAwait(false)` en código de librería

## Ejemplo de Código Correcto

```csharp
// ❌ Incorrecto
_logger.LogInformation("Processing request {RequestId}", requestId);
if (name.StartsWith("test")) { ... }
await SomeMethod();

// ✅ Correcto  
_logger.ProcessingRequest(requestId); // Usa LoggerMessage
if (name.StartsWith("test", StringComparison.OrdinalIgnoreCase)) { ... }
await SomeMethod().ConfigureAwait(false);
```

## Integración en CI/CD

Estos tests se ejecutan automáticamente en el pipeline y fallarán si:
- Se añaden nuevas violaciones que superen los baselines
- Se introduce código que viole los estándares más estrictos

Esto asegura que el código nuevo mantenga la calidad y que los problemas solucionados no vuelvan a aparecer.