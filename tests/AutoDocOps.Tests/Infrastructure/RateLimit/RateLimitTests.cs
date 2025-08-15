using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using AutoDocOps.WebAPI.Options;
using Xunit;
using FluentAssertions;

namespace AutoDocOps.Tests.Infrastructure.RateLimit;

public class RateLimitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StripeWebhook_RateLimited_Returns429_WhenBurstExceedsConfiguredLimit()
    {
        // Arrange
        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<RateLimitOptions>(options =>
                {
                    options.WebhookPerMinute = 2; // Very low limit for test
                });
            });
        }).CreateClient();

        var payload = """{"type": "test", "id": "evt_test"}""";
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act - Send requests exceeding limit  
        var response1 = await client.PostAsync("/stripe/webhook", content);
        var response2 = await client.PostAsync("/stripe/webhook", content);
        var response3 = await client.PostAsync("/stripe/webhook", content);

        // Assert - Third request should be rate limited
        response1.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        response2.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        response3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task StripeWebhook_RequestSize_RespectsConfiguredMaxBytes_Returns413_WhenExceeded()
    {
        // Arrange
        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<WebhookLimitsOptions>(options =>
                {
                    options.MaxBytes = 100; // Very small limit for test
                });
            });
        }).CreateClient();

        // Create payload larger than MaxBytes
        var largePayload = new string('x', 200);
        using var content = new StringContent(largePayload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/stripe/webhook", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }
}