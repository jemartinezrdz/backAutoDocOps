using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace AutoDocOps.Infrastructure.Services;

public class DocumentationGenerationService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DocumentationGenerationService> _logger;

    public DocumentationGenerationService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DocumentationGenerationService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Documentation Generation Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingPassports(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Check every 30 seconds
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in documentation generation service");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait before retrying
            }
        }

        _logger.LogInformation("Documentation Generation Service stopped");
    }

    private async Task ProcessPendingPassports(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var passportRepository = scope.ServiceProvider.GetRequiredService<IPassportRepository>();
        var projectRepository = scope.ServiceProvider.GetRequiredService<IProjectRepository>();

        var pendingPassports = await passportRepository.GetByStatusAsync(PassportStatus.Generating, cancellationToken);

        foreach (var passport in pendingPassports)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                await ProcessPassport(passport, projectRepository, passportRepository, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing passport {PassportId}", passport.Id);
                
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
        await SimulateDocumentationGeneration(passport, project, cancellationToken);

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

    private async Task SimulateDocumentationGeneration(Passport passport, Project project, CancellationToken cancellationToken)
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

        var phaseDelay = TimeSpan.FromSeconds(2); // Simulate processing time

        foreach (var phase in phases)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            _logger.LogDebug("Passport {PassportId}: {Phase}", passport.Id, phase);
            await Task.Delay(phaseDelay, cancellationToken);
        }
    }

    private string GenerateDocumentationContent(Project project)
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
