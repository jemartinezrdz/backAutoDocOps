using AutoDocOps.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AutoDocOps.Infrastructure.HealthChecks;

public class CacheHealthCheck : IHealthCheck
{
    private readonly ICacheService _cacheService;

    public CacheHealthCheck(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test cache connectivity by setting and getting a test value
            var testKey = "healthcheck_cache_test";
            var testValue = DateTime.UtcNow.ToString();
            
            await _cacheService.SetAsync(testKey, testValue, TimeSpan.FromSeconds(10), cancellationToken);
            var retrievedValue = await _cacheService.GetAsync<string>(testKey, cancellationToken);
            
            if (retrievedValue == testValue)
            {
                await _cacheService.RemoveAsync(testKey, cancellationToken);
                
                var data = new Dictionary<string, object>
                {
                    { "cache_type", "Redis" },
                    { "status", "connected" },
                    { "checked_at", DateTime.UtcNow }
                };

                return HealthCheckResult.Healthy("Cache is working correctly", data);
            }
            else
            {
                return HealthCheckResult.Degraded("Cache returned unexpected value");
            }
        }
        catch (Exception ex)
        {
            var data = new Dictionary<string, object>
            {
                { "cache_type", "Redis" },
                { "status", "error" },
                { "error", ex.Message },
                { "checked_at", DateTime.UtcNow }
            };

            return HealthCheckResult.Unhealthy("Cache service is not available", ex, data);
        }
    }
}
