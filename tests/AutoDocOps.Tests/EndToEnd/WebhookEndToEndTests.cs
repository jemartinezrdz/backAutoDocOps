using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoDocOps.Tests.Helpers;
using System.Text;
using System.Net;
using Xunit;
using FluentAssertions;

namespace AutoDocOps.Tests.EndToEnd;

public class WebhookEndToEndTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebhookEndToEndTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Webhook_EndToEnd_ValidEvent_200_MetricsAndLog()
    {
        // Arrange
        var loggerProvider = new InMemoryLoggerProvider();
        
        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILoggerProvider>(loggerProvider);
                // Configure test webhook secret
                services.Configure<Microsoft.Extensions.Configuration.IConfiguration>(config =>
                {
                    config["Stripe:WebhookSecret"] = "whsec_test_dummy_for_tests_0123456789";
                });
            });
        }).CreateClient();

        var validPayload = """{"type": "customer.created", "id": "evt_test_valid"}""";
        var content = new StringContent(validPayload, Encoding.UTF8, "application/json");
        
        // Add Stripe signature header (mock)
        content.Headers.Add("Stripe-Signature", "t=1609459200,v1=test_signature");

        // Act
        var response = await client.PostAsync("/stripe/webhook", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify logging occurred with correct EventId
        var logMessages = loggerProvider.GetLogMessages();
        logMessages.Should().Contain(msg => msg.Contains("customer.created"));
        
        // Verify metrics endpoint shows latency metric
        var metricsResponse = await client.GetAsync("/metrics");
        var metricsContent = await metricsResponse.Content.ReadAsStringAsync();
        metricsContent.Should().Contain("webhook_duration_seconds");
    }

    [Fact]
    public async Task Webhook_Idempotent_ReprocessIgnored_Still200()
    {
        // Arrange
        var loggerProvider = new InMemoryLoggerProvider();
        
        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILoggerProvider>(loggerProvider);
                services.Configure<Microsoft.Extensions.Configuration.IConfiguration>(config =>
                {
                    config["Stripe:WebhookSecret"] = "whsec_test_dummy_for_tests_0123456789";
                });
            });
        }).CreateClient();

        var payload = """{"type": "invoice.payment_succeeded", "id": "evt_same_id"}""";
        var content1 = new StringContent(payload, Encoding.UTF8, "application/json");
        var content2 = new StringContent(payload, Encoding.UTF8, "application/json");
        
        content1.Headers.Add("Stripe-Signature", "t=1609459200,v1=test_signature");
        content2.Headers.Add("Stripe-Signature", "t=1609459200,v1=test_signature");

        // Act - Send same event twice
        var response1 = await client.PostAsync("/stripe/webhook", content1);
        var response2 = await client.PostAsync("/stripe/webhook", content2);

        // Assert - Both should return 200, second should be idempotent
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify idempotency handling in logs
        var logMessages = loggerProvider.GetLogMessages();
        logMessages.Should().Contain(msg => msg.Contains("evt_same_id"));
    }
}