using Grpc.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using AutoDocOps.ILScanner.Logging;
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
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(context);

        try
        {
            _logger.StartingProjectAnalysis(request.ProjectName);

            var metadata = await _roslynAnalyzer.AnalyzeProjectAsync(
                request.ProjectPath,
                request.ProjectName,
                request.SourceFiles.ToList(),
                request.TargetFramework,
                context.CancellationToken).ConfigureAwait(false);

            _logger.ProjectAnalysisCompleted(request.ProjectName);

            return new AnalyzeProjectResponse
            {
                Success = true,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.ProjectAnalysisError(ex, request.ProjectName);
            
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
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(context);

        try
        {
            _logger.StartingSqlAnalysis(request.DatabaseType);

            var metadata = await _sqlAnalyzer.AnalyzeSqlAsync(
                request.SqlContent,
                request.DatabaseType,
                context.CancellationToken).ConfigureAwait(false);

            _logger.SqlAnalysisCompleted();

            return new AnalyzeSqlResponse
            {
                Success = true,
                Metadata = metadata
            };
        }
        catch (Exception ex)
        {
            _logger.SqlAnalysisError(ex);
            
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

