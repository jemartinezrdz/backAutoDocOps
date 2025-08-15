using System.Diagnostics.Metrics;

namespace AutoDocOps.Infrastructure.Monitoring;

public sealed class WebhookMetrics : IWebhookMetrics, IDisposable
{
    private readonly Counter<int> _requests;
    private readonly Counter<int> _invalid;
    private readonly Counter<int> _processed;

    private readonly Meter _meter;
    
    public WebhookMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);
        
        _meter = meterFactory.Create("AutoDocOps.Webhook", "1.0.0");
        
        _requests = _meter.CreateCounter<int>(
            name: "webhook_requests_total",
            unit: null,
            description: "Total webhook requests received");

        _invalid = _meter.CreateCounter<int>(
            name: "webhook_invalid_total", 
            unit: null,
            description: "Invalid webhook requests by reason");

        _processed = _meter.CreateCounter<int>(
            name: "webhook_processed_total",
            unit: null, 
            description: "Successfully processed webhooks");
    }

    public void ObserveRequest(string source)
    {
        _requests.Add(1, new KeyValuePair<string, object?>("source", source));
    }

    public void ObserveInvalid(string source, string reason)
    {
        _invalid.Add(1, 
            new KeyValuePair<string, object?>("source", source),
            new KeyValuePair<string, object?>("reason", reason));
    }

    public void ObserveProcessed(string source, string outcome)
    {
        _processed.Add(1,
            new KeyValuePair<string, object?>("source", source),
            new KeyValuePair<string, object?>("outcome", outcome));
    }

    public void Dispose()
    {
        _meter?.Dispose();
    }
}