using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using Microsoft.Extensions.Logging;

namespace AutoDocOps.Tests.Infrastructure.CodeQuality;

/// <summary>
/// Tests to ensure code quality standards are maintained and prevent regression of fixed issues
/// </summary>
public class CodeStandardsTests
{
    private static readonly Lazy<string> _solutionPath = new(() =>
    {
        // 1) Override opcional en CI/local
        var env = Environment.GetEnvironmentVariable("SOLUTION_ROOT");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env)) return Path.GetFullPath(env);

        // 2) Base estable: ubicación del ensamblado de tests
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        const int maxHops = 12; // cota dura para evitar recorridos largos
        int hops = 0;

        while (dir != null && hops++ < maxHops)
        {
            // Preferencias de marcador de raíz
            if (dir.GetFiles("*.sln").Length > 0) return dir.FullName;
            if (dir.GetDirectories(".git").Length > 0) return dir.FullName;
            if (dir.GetFiles("Directory.Build.props").Length > 0) return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Solution root not found within bounded search.");
    });

    private readonly string[] _sourceDirectories;

    public CodeStandardsTests()
    {
        _sourceDirectories = new[]
        {
            Path.Combine(GetSolutionPath(), "src"),
        };
    }

    private static string GetSolutionPath() => _solutionPath.Value;

    [Fact]
    public void ShouldNotUseDirectLoggerCallsCa1848Prevention()
    {
        var violations = new List<string>();
        var sourceFiles = GetCSharpFiles(_sourceDirectories);

        // Pattern to detect direct logger calls that should use LoggerMessage
        var directLoggerPattern = new Regex(
            @"_logger\.(LogDebug|LogInformation|LogWarning|LogError|LogCritical|LogTrace)\s*\(",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        // Allowed exceptions (test files, specific patterns that are acceptable)
        var allowedExceptions = new[]
        {
            "Tests.cs", // Test files are allowed
            "TestHelper", // Test helper files
            "ILScanner", // IL Scanner services can use direct logging
            "SqlAnalyzer", // SQL analyzer can use direct logging
        };

        foreach (var file in sourceFiles)
        {
            // Skip test files and other allowed exceptions
            if (allowedExceptions.Any(exception => file.Contains(exception)))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (directLoggerPattern.IsMatch(line) && !IsInTestOrCommentContext(line))
                {
                    violations.Add($"{Path.GetFileName(file)}:{i + 1} - Direct logger call found: {line.Trim()}");
                }
            }
        }

        // Allow some violations for existing code but prevent regression
        Assert.True(violations.Count < StandardsLimits.MaxDirectLoggerViolations, // Current baseline tracked via env override
            $"Found {violations.Count} direct logger calls that should use LoggerMessage delegates (CA1848). " +
            $"New WebAPI/Infrastructure code should use LoggerMessage pattern. Baseline limit: {StandardsLimits.MaxDirectLoggerViolations}.\n" +
            string.Join("\n", violations.Take(5)) + 
            (violations.Count > 5 ? $"\n... and {violations.Count - 5} more" : ""));
    }

    [Fact]
    public void ShouldHaveArgumentNullValidationCa1062Prevention()
    {
        var violations = new List<string>();
        var sourceFiles = GetCSharpFiles(_sourceDirectories);

        // For this test, we'll focus on detecting controllers that should have null validation
        // but we'll be more permissive since ASP.NET Core model binding handles most cases
        
        foreach (var file in sourceFiles)
        {
            // Only check controllers that handle complex input
            if (!file.Contains("Controller") || file.Contains("Tests.cs") || IsExemptController(file))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            
            // Look for POST/PUT methods that take complex objects but don't validate
            var complexInputMethods = new Regex(
                @"\[Http(?:Post|Put)\][\s\S]*?public[\s\S]*?\([^)]*(?:command|request|model)[^)]*\)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline
            );

            var hasNullChecks = content.Contains("ArgumentNullException.ThrowIfNull") ||
                               content.Contains("ThrowIfNull");

            var complexMethods = complexInputMethods.Matches(content);

            // Only flag if there are complex input methods but no validation patterns
            if (complexMethods.Count > 0 && !hasNullChecks)
            {
                violations.Add($"{Path.GetFileName(file)} - Controller with complex input methods but no null validation patterns");
            }
        }

        // Make this a warning rather than a hard failure for existing code
    Assert.True(violations.Count <= StandardsLimits.MaxControllerNullValidationViolations,
            $"Found {violations.Count} potential CA1062 violations (missing null validation). " +
            "New controllers should validate complex inputs:\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void ShouldUseInvariantCultureCa1304Ca1311Prevention()
    {
        var violations = new List<string>();
        var sourceFiles = GetCSharpFiles(_sourceDirectories);

        // Patterns to detect culture-specific operations
        var problematicPatterns = new[]
        {
            new Regex(@"\.ToLower\(\)\s", RegexOptions.Compiled), // Should use ToLowerInvariant()
            new Regex(@"\.ToUpper\(\)\s", RegexOptions.Compiled), // Should use ToUpperInvariant()
            new Regex(@"\.StartsWith\s*\(\s*[""'][^""']+[""']\s*\)", RegexOptions.Compiled), // Should specify StringComparison
            new Regex(@"\.EndsWith\s*\(\s*[""'][^""']+[""']\s*\)", RegexOptions.Compiled), // Should specify StringComparison
            new Regex(@"\.Contains\s*\(\s*[""'][^""']+[""']\s*\)", RegexOptions.Compiled) // Should specify StringComparison for .NET 5+
        };

        var patternNames = new[]
        {
            "ToLower() without Invariant",
            "ToUpper() without Invariant", 
            "StartsWith() without StringComparison",
            "EndsWith() without StringComparison",
            "Contains() without StringComparison"
        };

        foreach (var file in sourceFiles)
        {
            // Skip test files
            if (file.Contains("Tests.cs"))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var lines = content.Split('\n');

            for (int patternIndex = 0; patternIndex < problematicPatterns.Length; patternIndex++)
            {
                var pattern = problematicPatterns[patternIndex];
                var patternName = patternNames[patternIndex];

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (pattern.IsMatch(line) && !IsInTestOrCommentContext(line))
                    {
                        violations.Add($"{Path.GetFileName(file)}:{i + 1} - {patternName}: {line.Trim()}");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Found {violations.Count} culture-specific operations that should use invariant culture (CA1304/CA1311):\n" +
            string.Join("\n", violations.Take(10)) +
            (violations.Count > 10 ? $"\n... and {violations.Count - 10} more" : ""));
    }

    [Fact]
    public void ShouldUseConfigureAwaitFalseCa2007Prevention()
    {
        var violations = new List<string>();
        var sourceFiles = GetCSharpFiles(_sourceDirectories);

        // Pattern to detect await calls without ConfigureAwait(false) - more specific
        var awaitPattern = new Regex(
            @"await\s+[^;\n]+?(?<!\.ConfigureAwait\s*\(\s*false\s*\))\s*;",
            RegexOptions.Compiled | RegexOptions.Singleline
        );

        // Exception patterns (these are acceptable)
        var exceptionPatterns = new[]
        {
            new Regex(@"await\s+Task\.Delay", RegexOptions.Compiled),
            new Regex(@"await\s+using", RegexOptions.Compiled),
            new Regex(@"await\s+foreach", RegexOptions.Compiled),
            new Regex(@"await\s+_mediator\.Send", RegexOptions.Compiled), // MediatR calls in controllers are fine
            new Regex(@"app\.RunAsync", RegexOptions.Compiled) // App startup calls
        };

        foreach (var file in sourceFiles)
        {
            // Skip test files, controllers, and Program.cs (ASP.NET Core doesn't need ConfigureAwait(false))
            if (file.Contains("Tests.cs") || file.Contains("Controller") || file.Contains("Program.cs"))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (awaitPattern.IsMatch(line) && 
                    !exceptionPatterns.Any(p => p.IsMatch(line)) &&
                    !IsInTestOrCommentContext(line))
                {
                    violations.Add($"{Path.GetFileName(file)}:{i + 1} - await without ConfigureAwait(false): {line.Trim()}");
                }
            }
        }

        // For now, allow some violations since this is a preventive test
    Assert.True(violations.Count < StandardsLimits.MaxAwaitWithoutConfigureAwaitViolations,
            $"Found {violations.Count} await calls that should use ConfigureAwait(false) (CA2007). " +
            "New code should use ConfigureAwait(false) in library code:\n" +
            string.Join("\n", violations.Take(5)) +
            (violations.Count > 5 ? $"\n... and {violations.Count - 5} more" : ""));
    }

    [Fact]
    public void ShouldHaveLoggerMessageDefinitions()
    {
        var sourceFiles = GetCSharpFiles(_sourceDirectories);
        var loggingClassFound = false;
        var loggerMessageAttributesFound = 0;

        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            
            // Look for Logging class
            if (content.Contains("static partial class Log") || content.Contains("static partial class Logging"))
            {
                loggingClassFound = true;
            }

            // Count LoggerMessage attributes
            var loggerMessageMatches = Regex.Matches(content, @"\[LoggerMessage\(", RegexOptions.IgnoreCase);
            loggerMessageAttributesFound += loggerMessageMatches.Count;
        }

        Assert.True(loggingClassFound, "LoggerMessage static partial class not found. This is required for CA1848 compliance.");
        Assert.True(loggerMessageAttributesFound >= 10, 
            $"Expected at least 10 LoggerMessage definitions, but found {loggerMessageAttributesFound}. " +
            "This suggests LoggerMessage pattern is not being used consistently.");
    }

    [Fact]
    public void ShouldNotHaveHardcodedStringsInLogging()
    {
        var violations = new List<string>();
        var sourceFiles = GetCSharpFiles(_sourceDirectories);

        // Pattern to detect hardcoded strings in logging (should use LoggerMessage)
        var hardcodedLogPattern = new Regex(
            @"_logger\.Log[A-Za-z]*\s*\(\s*[""'][^""']+[""']",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        // Files that are allowed to use direct logging
        var allowedFiles = new[]
        {
            "Tests.cs",
            "ILScanner", // IL Scanner services
            "SqlAnalyzer", // SQL analyzer
            "RoslynAnalyzer" // Roslyn analyzer
        };

        foreach (var file in sourceFiles)
        {
            if (allowedFiles.Any(allowed => file.Contains(allowed)))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (hardcodedLogPattern.IsMatch(line))
                {
                    violations.Add($"{Path.GetFileName(file)}:{i + 1} - Hardcoded string in logging: {line.Trim()}");
                }
            }
        }

        // Allow some violations for existing code but prevent regression
        Assert.True(violations.Count < StandardsLimits.MaxHardcodedLogStringViolations,
            $"Found {violations.Count} hardcoded strings in logging calls. " +
            $"New WebAPI/Infrastructure code should use LoggerMessage delegates instead. Baseline limit: {StandardsLimits.MaxHardcodedLogStringViolations}.\n" +
            string.Join("\n", violations.Take(5)) +
            (violations.Count > 5 ? $"\n... and {violations.Count - 5} more" : ""));
    }

    private static IEnumerable<string> GetCSharpFiles(string[] directories) =>
        directories
            .Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.cs", SearchOption.AllDirectories))
            .Where(p => !p.Contains("\\bin\\") && !p.Contains("\\obj\\"))
            .Where(p => !p.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Where(p => !p.Contains("\\Migrations\\"));

    private static IEnumerable<string> GetProductionCSharpFiles()
    {
        var solutionPath = _solutionPath.Value;
        var productionDirectories = new[]
        {
            Path.Combine(solutionPath, "src")
        };
        
        return GetCSharpFiles(productionDirectories)
            .Where(f => !f.Contains("Tests.cs") && 
                       !f.Contains("\\Migrations\\") &&
                       !f.Contains("\\TestHelper"));
    }

    private static bool IsInTestOrCommentContext(string line)
    {
        // For line-by-line analysis, we still use the heuristic approach
        // as it's sufficient for most cases and more performant
        var trimmedLine = line.Trim();
        if (trimmedLine.StartsWith("//", StringComparison.Ordinal) || 
            trimmedLine.StartsWith("/*", StringComparison.Ordinal) || 
            trimmedLine.StartsWith('*') || 
            trimmedLine.Contains("Test", StringComparison.Ordinal))
        {
            return true;
        }
        return false;
    }

    private static bool IsInTestOrCommentContext(string filePath, int position)
    {
        var text = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetRoot();

        // 1) Comentarios
        var trivia = root.FindTrivia(position, findInsideTrivia: true);
        if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            return true;

        // 2) Test methods o clases *Tests
        var token = root.FindToken(position);
        var method = token.Parent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        var type = token.Parent?.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        bool inTestMethod = method?.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString().Contains("Fact") || a.Name.ToString().Contains("Theory")) == true;

        bool inTestsClass = type?.Identifier.ValueText.EndsWith("Tests", StringComparison.Ordinal) == true;

        return inTestMethod || inTestsClass;
    }

    private static bool IsExemptController(string filePath)
    {
        // Controllers that are exempt from null validation requirements
        var exemptControllers = new[]
        {
            "HealthController", // Health check controllers typically don't need validation
            "TestController" // Test controllers
        };

        return exemptControllers.Any(exempt => filePath.Contains(exempt));
    }

    [Fact]
    public void NoInterpolatedLoggingInProductionCode()
    {
        var violations = new List<string>();
        
        // Recorre archivos *.cs en src/ (excluye tests/migrations)
        foreach (var file in GetProductionCSharpFiles())
        {
            var text = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(text);
            var root = tree.GetRoot();

            // Busca invocaciones logger.LogXxx(...)
            var invocations = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => inv.Expression is MemberAccessExpressionSyntax maes &&
                              maes.Name.Identifier.ValueText.StartsWith("Log", StringComparison.Ordinal));

            foreach (var call in invocations)
            {
                // ¿Argumento 1 es expresión interpolada?  logger.LogInformation($"...")
                var args = call.ArgumentList?.Arguments;
                if (args.HasValue && args.Value.Count > 0)
                {
                    var first = args.Value[0].Expression;
                    if (first is InterpolatedStringExpressionSyntax)
                    {
                        var location = call.GetLocation().GetLineSpan();
                        violations.Add($"Interpolated logging found in {Path.GetFileName(file)} at line {location.StartLinePosition.Line + 1}");
                    }
                }
            }
        }
        
        Assert.True(violations.Count == 0,
            $"Found {violations.Count} interpolated logging calls that should use LoggerMessage pattern:\n" +
            string.Join("\n", violations.Take(10)) +
            (violations.Count > 10 ? $"\n... and {violations.Count - 10} more" : ""));
    }
}

public static class StandardsLimits
{
    // Defaults (baseline documented in LEEME)
    public const int DefaultMaxDirectLoggerViolations = 70; // baseline ~59
    public const int DefaultMaxHardcodedLogStringViolations = 40; // baseline ~35
    public const int DefaultMaxAwaitWithoutConfigureAwaitViolations = 100;
    public const int DefaultMaxControllerNullValidationViolations = 3;

    public static int MaxDirectLoggerViolations => Get("MAX_DIRECT_LOGGER", DefaultMaxDirectLoggerViolations);
    public static int MaxHardcodedLogStringViolations => Get("MAX_HARDCODED_LOG_STRINGS", DefaultMaxHardcodedLogStringViolations);
    public static int MaxAwaitWithoutConfigureAwaitViolations => Get("MAX_AWAIT_NO_CONFIGUREAWAIT", DefaultMaxAwaitWithoutConfigureAwaitViolations);
    public static int MaxControllerNullValidationViolations => Get("MAX_CONTROLLER_NULL_VALIDATION", DefaultMaxControllerNullValidationViolations);

    private static int Get(string env, int fallback) => int.TryParse(Environment.GetEnvironmentVariable(env), out var v) ? v : fallback;
}