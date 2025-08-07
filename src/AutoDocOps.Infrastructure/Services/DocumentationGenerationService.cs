using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AutoDocOps.Infrastructure.Services;

public class DocumentationGenerationService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DocumentationGenerationService> _logger;
    private readonly DocumentationGenerationOptions _options;
    private int _failureCount = 0;
    private TimeSpan _currentRetryDelay;
    private readonly TimeSpan _maxRetryDelay = TimeSpan.FromHours(1); // Maximum retry delay

    public DocumentationGenerationService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DocumentationGenerationService> logger,
        IOptions<DocumentationGenerationOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _options = options.Value;
        _currentRetryDelay = TimeSpan.FromMinutes(_options.RetryDelayMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Documentation Generation Service started at {StartTime}", DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var activity = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["Operation"] = "ProcessPendingPassports",
                    ["Timestamp"] = DateTime.UtcNow
                });

                await ProcessPendingPassports(stoppingToken);
                // Reset failure count on successful processing
                _failureCount = 0;
                _currentRetryDelay = TimeSpan.FromMinutes(_options.RetryDelayMinutes);
                await Task.Delay(TimeSpan.FromSeconds(_options.CheckIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Documentation Generation Service is shutting down gracefully");
                break;
            }
            catch (Exception ex)
            {
                _failureCount++;
                // Exponential backoff: double the delay, up to maxRetryDelay using TotalMilliseconds to prevent overflow
                var nextDelayMs = Math.Min(_currentRetryDelay.TotalMilliseconds * 2, _maxRetryDelay.TotalMilliseconds);
                _currentRetryDelay = TimeSpan.FromMilliseconds(nextDelayMs);
                _logger.LogError(ex, "Critical error in documentation generation service at {ErrorTime}. Retrying after {RetryDelay} (attempt {FailureCount})", DateTime.UtcNow, _currentRetryDelay, _failureCount);
                await Task.Delay(_currentRetryDelay, stoppingToken);
            }
        }

        _logger.LogInformation("Documentation Generation Service stopped at {StopTime}", DateTime.UtcNow);
    }

    private async Task ProcessPendingPassports(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var passportRepository = scope.ServiceProvider.GetRequiredService<IPassportRepository>();
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

        var pendingPassports = await passportRepository.GetByStatusAsync(PassportStatus.Generating, cancellationToken);
        
        _logger.LogInformation("Found {PendingCount} passports to process", pendingPassports.Count());

        foreach (var passport in pendingPassports)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                using var passportScope = _logger.BeginScope(new Dictionary<string, object>
                {
                    ["PassportId"] = passport.Id,
                    ["ProjectId"] = passport.ProjectId,
                    ["Operation"] = "ProcessPassport"
                });

                await ProcessPassport(passport, projectRepository, passportRepository, cancellationToken);
                
                stopwatch.Stop();
                _logger.LogInformation("Successfully processed passport {PassportId} in {ProcessingTimeMs}ms", 
                    passport.Id, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to process passport {PassportId} after {ProcessingTimeMs}ms: {ErrorMessage}", 
                    passport.Id, stopwatch.ElapsedMilliseconds, ex.Message);
                
                // Mark as failed
                passport.Status = PassportStatus.Failed;
                passport.CompletedAt = DateTime.UtcNow;
                passport.ErrorMessage = ex.Message;
                await passportRepository.UpdateAsync(passport, cancellationToken);
            }
        }
    }

    private async Task ProcessPassport(
        Passport passport, 
        IProjectRepository projectRepository, 
        IPassportRepository passportRepository, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing passport {PassportId} for project {ProjectId}", 
            passport.Id, passport.ProjectId);

        // Get project details
        var project = await projectRepository.GetByIdWithSpecsAsync(passport.ProjectId, cancellationToken);
        if (project == null)
        {
            throw new InvalidOperationException($"Project {passport.ProjectId} not found");
        }

        // Simulate documentation generation process
        if (_options.EnableSimulation)
        {
            await SimulateDocumentationGeneration(passport, cancellationToken);
        }

        // Mark as completed
        passport.Status = PassportStatus.Completed;
        passport.CompletedAt = DateTime.UtcNow;
        passport.DocumentationContent = GenerateDocumentationContent(project);
        passport.SizeInBytes = System.Text.Encoding.UTF8.GetByteCount(passport.DocumentationContent);

        // Add metadata
        var metadata = new
        {
            GenerationMethod = "Automated",
            ProcessingTime = DateTime.UtcNow - passport.GeneratedAt,
            SpecsAnalyzed = project.Specs.Count,
            GeneratedAt = DateTime.UtcNow
        };
        passport.Metadata = JsonSerializer.Serialize(metadata);

        await passportRepository.UpdateAsync(passport, cancellationToken);

        _logger.LogInformation("Completed processing passport {PassportId}", passport.Id);
    }

    private async Task SimulateDocumentationGeneration(Passport passport, CancellationToken cancellationToken)
    {
        // Simulate different phases of documentation generation
        var phases = new[]
        {
            "Analyzing project structure...",
            "Extracting code metadata...",
            "Processing specifications...",
            "Generating documentation content...",
            "Formatting output..."
        };

        var phaseDelay = TimeSpan.FromSeconds(_options.SimulationPhaseDelaySeconds);

        foreach (var phase in phases)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            _logger.LogDebug("Passport {PassportId}: {Phase}", passport.Id, phase);
            await Task.Delay(phaseDelay, cancellationToken);
        }
    }

    private static string GenerateDocumentationContent(Project project)
    {
        var content = $@"# {project.Name} Documentation

## Project Overview
- **Name**: {project.Name}
- **Description**: {project.Description ?? "No description provided"}
- **Repository**: {project.RepositoryUrl}
- **Branch**: {project.Branch}
- **Created**: {project.CreatedAt:yyyy-MM-dd HH:mm:ss}
- **Last Updated**: {project.UpdatedAt:yyyy-MM-dd HH:mm:ss}

## Project Statistics
- **Total Specifications**: {project.Specs.Count}
- **Total Lines**: {project.Specs.Sum(s => s.LineCount)}
- **Total Size**: {project.Specs.Sum(s => s.SizeInBytes)} bytes

## File Structure
{string.Join("\n", project.Specs.Select(s => $"- {s.FilePath} ({s.Language}) - {s.LineCount} lines"))}

## Specifications

{string.Join("\n\n", project.Specs.Select(s => $@"### {s.FileName}
- **Path**: {s.FilePath}
- **Language**: {s.Language}
- **Type**: {s.FileType}
- **Lines**: {s.LineCount}
- **Size**: {s.SizeInBytes} bytes
- **Created**: {s.CreatedAt:yyyy-MM-dd HH:mm:ss}

```{s.Language.ToLower()}
{(s.Content.Length > 1000 ? s.Content.Substring(0, 1000) + "..." : s.Content)}
```"))}

---
*Documentation generated automatically by AutoDocOps on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*
";

        return content;
    }
}
