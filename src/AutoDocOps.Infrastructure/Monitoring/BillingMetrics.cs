using System.Diagnostics.Metrics;

namespace AutoDocOps.Infrastructure.Monitoring;

internal static class BillingMetrics
{
    internal static readonly Meter Meter = new("AutoDocOps.Billing", "1.0.0");

    internal static readonly Counter<long> Operations = Meter.CreateCounter<long>(
        name: "billing_operations_total",
        unit: null,
        description: "Total billing operations by type and result");

    internal static readonly Histogram<double> OperationLatencySeconds = Meter.CreateHistogram<double>(
        name: "billing_operation_latency_seconds",
        unit: "s",
        description: "Latency of billing operations in seconds");
}
