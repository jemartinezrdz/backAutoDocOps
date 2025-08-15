using AutoDocOps.Domain.Enums;

namespace AutoDocOps.Domain.Entities;

public class BillingAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime UtcTimestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public SecretSource SecretSource { get; set; } = SecretSource.Missing;
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string Outcome { get; set; } = string.Empty; // ok|fail
    public double? LatencyMs { get; set; }
}
