using System.Diagnostics.Metrics;

namespace AutoDocOps.WebAPI.Metrics;

public interface IWebhookMetrics
{
    void ObserveLatency(string provider, TimeSpan elapsed);
    void ObserveInvalid(string provider, string reason);
    void ObserveTimeout(string provider);
    void ObserveRequest(string provider);
    void ObserveProcessed(string provider, string status);
}

public sealed class PromWebhookMetrics : IWebhookMetrics
{
    private static readonly Meter Meter = new("AutoDocOps.Webhook");
    
    private static readonly Histogram<double> Latency = Meter.CreateHistogram<double>(
        "webhook_duration_seconds", 
        "s", 
        "Webhook duration in seconds");

    private static readonly Counter<long> Invalid = Meter.CreateCounter<long>(
        "webhook_invalid_total", 
        "requests", 
        "Invalid webhook events");

    private static readonly Counter<long> Timeout = Meter.CreateCounter<long>(
        "webhook_timeout_total", 
        "requests", 
        "Webhook timeouts");

    private static readonly Counter<long> Request = Meter.CreateCounter<long>(
        "webhook_request_total", 
        "requests", 
        "Total webhook requests");

    public void ObserveLatency(string provider, TimeSpan elapsed) 
        => Latency.Record(elapsed.TotalSeconds, new KeyValuePair<string, object?>("provider", provider));
        
    public void ObserveInvalid(string provider, string reason)   
        => Invalid.Add(1, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("reason", reason));
        
    public void ObserveTimeout(string provider)                  
        => Timeout.Add(1, new KeyValuePair<string, object?>("provider", provider));

    public void ObserveRequest(string provider)                  
        => Request.Add(1, new KeyValuePair<string, object?>("provider", provider));

    public void ObserveProcessed(string provider, string status)
        => Request.Add(1, new KeyValuePair<string, object?>("provider", provider), new KeyValuePair<string, object?>("status", status));
}