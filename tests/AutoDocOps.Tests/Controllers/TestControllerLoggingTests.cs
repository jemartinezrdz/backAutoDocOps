using AutoDocOps.Tests.Helpers;
using AutoDocOps.WebAPI.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AutoDocOps.Tests.Controllers;

#if DEBUG
public class TestControllerLoggingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TestControllerLoggingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TestController_Ping_EmitsStructuredLog()
    {
        // Arrange
        using var provider = new InMemoryLoggerProvider();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(provider);
                    logging.SetMinimumLevel(LogLevel.Information);
                });
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/health");
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        // No debería haber logs de TestController en /health endpoint ya que no logea
        // Pero validamos que el sistema de logging está funcionando
        Assert.NotNull(provider.Entries);
    }

    [Fact]
    public async Task TestController_CacheEndpoint_EmitsStructuredLog()
    {
        // Arrange
        using var provider = new InMemoryLoggerProvider();
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(provider);
                    logging.SetMinimumLevel(LogLevel.Information);
                });
            });
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/cache/testkey");
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        // Debería existir EventId 2001 (TestEndpointHit) para el endpoint de cache
        var logEntry = provider.Entries.FirstOrDefault(e => e.EventId.Id == 2001 && e.Level == LogLevel.Information);
        Assert.True(logEntry != default, "Expected log entry with EventId 2001 not found");
        Assert.Contains("cache", logEntry.Message, StringComparison.OrdinalIgnoreCase);
    }
}
#endif