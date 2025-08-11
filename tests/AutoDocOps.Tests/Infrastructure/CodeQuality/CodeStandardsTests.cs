using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Microsoft.Extensions.Logging;

namespace AutoDocOps.Tests.Infrastructure.CodeQuality;

/// <summary>
/// Tests to ensure code quality standards are maintained and prevent regression of fixed issues
/// </summary>
public class CodeStandardsTests
{
    private readonly string _solutionPath;
    private readonly string[] _sourceDirectories;

    public CodeStandardsTests()
    {
        _solutionPath = GetSolutionPath();
        _sourceDirectories = new[]
        {
            Path.Combine(_solutionPath, "src"),
        };
    }

    [Fact]
    public void ShouldNotUseDirectLoggerCalls_CA1848_Prevention()
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
                continue;

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
        Assert.True(violations.Count < 70, // Current baseline: ~59 violations
            $"Found {violations.Count} direct logger calls that should use LoggerMessage delegates (CA1848). " +
            "New WebAPI/Infrastructure code should use LoggerMessage pattern. Current baseline: 59 violations.\n" +
            string.Join("\n", violations.Take(5)) + 
            (violations.Count > 5 ? $"\n... and {violations.Count - 5} more" : ""));
    }

    [Fact]
    public void ShouldHaveArgumentNullValidation_CA1062_Prevention()
    {
        var violations = new List<string>();
        var sourceFiles = GetCSharpFiles(_sourceDirectories);

        // For this test, we'll focus on detecting controllers that should have null validation
        // but we'll be more permissive since ASP.NET Core model binding handles most cases
        
        foreach (var file in sourceFiles)
        {
            // Only check controllers that handle complex input
            if (!file.Contains("Controller") || file.Contains("Tests.cs") || IsExemptController(file))
                continue;

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
        Assert.True(violations.Count <= 3, // Allow some existing violations
            $"Found {violations.Count} potential CA1062 violations (missing null validation). " +
            "New controllers should validate complex inputs:\n" +
            string.Join("\n", violations));
    }

    [Fact]
    public void ShouldUseInvariantCulture_CA1304_CA1311_Prevention()
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
                continue;

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
    public void ShouldUseConfigureAwaitFalse_CA2007_Prevention()
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
                continue;

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
        Assert.True(violations.Count < 100, // Set a reasonable threshold
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
    public void ShouldNotHaveHardcodedStrings_InLogging()
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
                continue;

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
        Assert.True(violations.Count < 40, // Current baseline: ~35 violations
            $"Found {violations.Count} hardcoded strings in logging calls. " +
            "New WebAPI/Infrastructure code should use LoggerMessage delegates instead. Current baseline: 35 violations.\n" +
            string.Join("\n", violations.Take(5)) +
            (violations.Count > 5 ? $"\n... and {violations.Count - 5} more" : ""));
    }

    private static string GetSolutionPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);

        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Solution file not found");
    }

    private static IEnumerable<string> GetCSharpFiles(string[] directories)
    {
        var files = new List<string>();
        
        foreach (var directory in directories)
        {
            if (Directory.Exists(directory))
            {
                files.AddRange(Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories));
            }
        }

        return files;
    }

    private static bool IsInTestOrCommentContext(string line)
    {
        var trimmedLine = line.Trim();
        return trimmedLine.StartsWith("//") || 
               trimmedLine.StartsWith("/*") || 
               trimmedLine.StartsWith("*") ||
               trimmedLine.Contains("Test");
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
}