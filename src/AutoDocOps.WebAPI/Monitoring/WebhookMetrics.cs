using System.Diagnostics.Metrics;

namespace AutoDocOps.WebAPI.Monitoring;

internal static class WebhookMetrics
{
    internal static readonly Meter Meter = new("AutoDocOps.Webhook", "1.0.0");
    internal static readonly Counter<int> Requests = Meter.CreateCounter<int>(
        name: "stripe_webhook_requests_total",
        unit: null,
        description: "Total Stripe webhook requests processed");

    internal static readonly Counter<int> Failures = Meter.CreateCounter<int>(
        name: "stripe_webhook_failures_total",
        unit: null,
        description: "Failed Stripe webhook requests by reason");

    internal static readonly Histogram<double> LatencySeconds = Meter.CreateHistogram<double>(
        name: "stripe_webhook_latency_seconds",
        unit: "s",
        description: "Stripe webhook processing latency in seconds");
}
