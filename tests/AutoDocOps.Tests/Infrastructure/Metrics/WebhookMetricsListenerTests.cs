using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AutoDocOps.Tests.Infrastructure.Metrics;

public class WebhookMetricsListenerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebhookMetricsListenerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(_ => { });
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
                if (instrument.Meter.Name == "AutoDocOps.Webhook" && instrument.Name == "stripe_webhook_requests_total")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<int>((instrument, value, tags, state) =>
        {
            if (instrument.Name == "stripe_webhook_requests_total")
            {
                System.Threading.Interlocked.Add(ref seen, value);
            }
        });
        listener.Start();

        // Missing signature triggers failure path but still increments requests
        var response = await client.PostAsync("/stripe/webhook", new StringContent("{}", System.Text.Encoding.UTF8, "application/json")).ConfigureAwait(true);
        response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.OK);

        // Simple wait for metric flush
        await Task.Delay(100);
        seen.Should().BeGreaterThan(0);
    }
}
