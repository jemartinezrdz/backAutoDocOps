using AutoDocOps.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AutoDocOps.Infrastructure.HealthChecks;

public class LlmHealthCheck : IHealthCheck
{
    private readonly ILlmClient _llmClient;

    public LlmHealthCheck(ILlmClient llmClient)
    {
        _llmClient = llmClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test LLM connectivity with a simple query
            var testQuery = "Hello";
            var response = await _llmClient.ChatAsync(testQuery, cancellationToken).ConfigureAwait(false);
            
            if (!string.IsNullOrEmpty(response))
            {
                var data = new Dictionary<string, object>
                {
                    { "llm_type", _llmClient.GetType().Name },
                    { "status", "connected" },
                    { "test_response_length", response.Length },
                    { "checked_at", DateTime.UtcNow }
                };

                return HealthCheckResult.Healthy("LLM service is working correctly", data);
            }
            else
            {
                return HealthCheckResult.Degraded("LLM returned empty response");
            }
        }
        catch (Exception ex)
        {
            var data = new Dictionary<string, object>
            {
                { "llm_type", _llmClient.GetType().Name },
                { "status", "error" },
                { "error", ex.Message },
                { "checked_at", DateTime.UtcNow }
            };

            // If it's a FakeLlmClient, it should always work
            if (_llmClient.GetType().Name == "FakeLlmClient")
            {
                return HealthCheckResult.Unhealthy("Fake LLM client should not fail", ex, data);
            }

            return HealthCheckResult.Degraded("LLM service is not available (but not critical)", ex, data);
        }
    }
}
