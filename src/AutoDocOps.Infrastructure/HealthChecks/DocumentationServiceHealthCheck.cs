using AutoDocOps.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AutoDocOps.Infrastructure.HealthChecks;

public class DocumentationServiceHealthCheck : IHealthCheck
{
    private readonly IPassportRepository _passportRepository;

    public DocumentationServiceHealthCheck(IPassportRepository passportRepository)
    {
        _passportRepository = passportRepository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if we can query the database
            var pendingCount = (await _passportRepository.GetByStatusAsync(
                Domain.Entities.PassportStatus.Generating, cancellationToken)).Count();

            var data = new Dictionary<string, object>
            {
                { "pending_passports", pendingCount },
                { "checked_at", DateTime.UtcNow }
            };

            // Warning if too many pending passports
            if (pendingCount > 10)
            {
                return HealthCheckResult.Degraded(
                    $"High number of pending passports: {pendingCount}", 
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "Documentation service is running normally", 
                data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Documentation service is not responding", 
                ex);
        }
    }
}
