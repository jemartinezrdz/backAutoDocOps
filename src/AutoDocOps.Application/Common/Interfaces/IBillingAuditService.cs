using AutoDocOps.Domain.Enums;

namespace AutoDocOps.Application.Common.Interfaces;

public interface IBillingAuditService
{
    Task LogAsync(string eventType, SecretSource secretSource, double? latencyMs = null, CancellationToken ct = default);
}
