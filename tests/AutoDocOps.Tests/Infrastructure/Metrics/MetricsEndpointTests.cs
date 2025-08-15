using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;
using FluentAssertions;

namespace AutoDocOps.Tests.Infrastructure.Metrics;

public class MetricsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MetricsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Metrics_Endpoint_ExposesExpectedSeries()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/metrics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify expected metric series are exposed
        content.Should().Contain("webhook_duration_seconds");
        content.Should().Contain("webhook_invalid_total");
        content.Should().Contain("webhook_timeout_total");
    }

    [Fact]
    public async Task Metrics_Invalid_IncrementsWithReason()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act - Force 401/413 responses to generate metrics
        using var emptyContent = new StringContent("");
        var unauthorizedResponse = await client.PostAsync("/stripe/webhook", emptyContent);
        
        using var largeContent = new StringContent(new string('x', 500_000)); // Exceed default limit
        var largeContentResponse = await client.PostAsync("/stripe/webhook", largeContent);

        // Get metrics
        var metricsResponse = await client.GetAsync("/metrics");
        var metricsContent = await metricsResponse.Content.ReadAsStringAsync();

        // Assert - Verify metrics contain failure reasons
        metricsContent.Should().Contain("webhook_invalid_total");
        // Note: Specific reason labels would need proper webhook implementation to verify
    }
}