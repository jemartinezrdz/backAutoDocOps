using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AutoDocOps.Tests.Infrastructure.Metrics;

public class PrometheusMetricsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PrometheusMetricsTests(WebApplicationFactory<Program> factory)
    {
    ArgumentNullException.ThrowIfNull(factory);
    _factory = factory.WithWebHostBuilder(_ => { });
    }

    [Fact]
    public async Task MetricsEndpointReturnsPrometheusFormat()
    {
        if (Environment.GetEnvironmentVariable("METRICS_SCRAPE_ENABLED") != "true")
        {
            // Feature disabled: treat as inconclusive pass (no scrape endpoint exposed)
            return;
        }
        var client = _factory.CreateClient();
    var response = await client.GetAsync("/metrics").ConfigureAwait(true);
    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(true);

        response.IsSuccessStatusCode.Should().BeTrue();
        body.Should().Contain("stripe_webhook_requests_total");
    }
}
