using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AutoDocOps.Tests.Infrastructure.Metrics;

public class WebhookMetricsListenerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebhookMetricsListenerTests(WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EnvironmentName"] = "Test",
                    ["Features:EnableBilling"] = "false",
                    ["Features:EnableSession"] = "false",
                    ["Features:EnableDocumentationGeneration"] = "false",
                    ["Caching:Provider"] = "Memory",
                    ["Stripe:SecretKey"] = "sk_test_dummy_for_tests_0123456789",
                    ["Stripe:WebhookSecret"] = "whsec_test_dummy_for_tests_0123456789",
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=autodocops_test;Username=postgres;Password=postgres"
                });
            });
        });
    }

    [Fact]
    public async Task PostingInvalidWebhook_IncrementsRequestCounter()
    {
        var client = _factory.CreateClient();
        var seen = 0;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == "AutoDocOps.Webhook" && instrument.Name == "webhook_requests_total")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<int>((instrument, value, tags, state) =>
        {
            if (instrument.Name == "webhook_requests_total")
            {
                System.Threading.Interlocked.Add(ref seen, value);
            }
        });
        listener.Start();

        // Missing signature triggers failure path but still increments requests
    using var payload = new StringContent(
        "{\"id\":\"evt_test\",\"type\":\"passport.generated\",\"data\":{\"project_id\":\"123\"}}", 
        System.Text.Encoding.UTF8, 
        "application/json");
    var response = await client.PostAsync("/stripe/webhook", payload).ConfigureAwait(true);
        response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.OK);

        // Simple wait for metric flush
        // Poll a few times to allow exporter to observe measurement
        for (var i = 0; i < 5 && seen == 0; i++)
        {
            await Task.Delay(50).ConfigureAwait(true);
        }
        seen.Should().BeGreaterThan(0, "posting an invalid webhook must increment request counter");
    }
}
