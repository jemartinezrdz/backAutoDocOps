using Grpc.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AutoDocOps.ILScanner.Services;

public class ILScannerGrpcService : ILScannerService.ILScannerServiceBase
{
    private readonly ILogger<ILScannerGrpcService> _logger;
    private readonly RoslynAnalyzer _roslynAnalyzer;
    private readonly SqlAnalyzer _sqlAnalyzer;

    public ILScannerGrpcService(
        ILogger<ILScannerGrpcService> logger,
        RoslynAnalyzer roslynAnalyzer,
        SqlAnalyzer sqlAnalyzer)
    {
        _logger = logger;
        _roslynAnalyzer = roslynAnalyzer;
        _sqlAnalyzer = sqlAnalyzer;
    }

    public override async Task<AnalyzeProjectResponse> AnalyzeProject(
        AnalyzeProjectRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Starting analysis for project: {ProjectName}", request.ProjectName);

            var metadata = await _roslynAnalyzer.AnalyzeProjectAsync(
                request.ProjectPath,
                request.ProjectName,
                request.SourceFiles.ToList(),
                request.TargetFramework,
                context.CancellationToken);

            _logger.LogInformation("Analysis completed successfully for project: {ProjectName}", request.ProjectName);

            return new AnalyzeProjectResponse
            {
                Success = true,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing project: {ProjectName}", request.ProjectName);
            
            return new AnalyzeProjectResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<AnalyzeSqlResponse> AnalyzeSql(
        AnalyzeSqlRequest request, 
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("Starting SQL analysis for database type: {DatabaseType}", request.DatabaseType);

            var metadata = await _sqlAnalyzer.AnalyzeSqlAsync(
                request.SqlContent,
                request.DatabaseType,
                context.CancellationToken);

            _logger.LogInformation("SQL analysis completed successfully");

            return new AnalyzeSqlResponse
            {
                Success = true,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing SQL content");
            
            return new AnalyzeSqlResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public override Task<HealthCheckResponse> HealthCheck(
        HealthCheckRequest request, 
        ServerCallContext context)
    {
        return Task.FromResult(new HealthCheckResponse
        {
            IsHealthy = true,
            Version = "1.0.0",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
    }
}

