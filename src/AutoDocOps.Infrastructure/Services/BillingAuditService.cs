using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Domain.Enums;
using AutoDocOps.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using System.Threading;
using AutoDocOps.Infrastructure.Logging;

namespace AutoDocOps.Infrastructure.Services;

internal sealed class BillingAuditService : IBillingAuditService
{
    private readonly ILogger<BillingAuditService> _logger;
    private readonly Channel<BillingAuditLog> _channel;

    public BillingAuditService(ILogger<BillingAuditService> logger, Channel<BillingAuditLog> channel)
    {
        _logger = logger;
        _channel = channel;
    }

    public Task LogAsync(string eventType, SecretSource secretSource, double? latencyMs = null, CancellationToken ct = default)
    {
        var entry = new BillingAuditLog
        {
            EventType = eventType,
            SecretSource = secretSource,
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
            Outcome = "ok", // At present only logged on success path
            LatencyMs = latencyMs
        };
        if (!_channel.Writer.TryWrite(entry))
        {
            _logger.AuditChannelFull(eventType);
        }
        return Task.CompletedTask;
    }
}

internal sealed class BillingAuditBackgroundService : BackgroundService
{
    private readonly Channel<BillingAuditLog> _channel;
    private readonly ILogger<BillingAuditBackgroundService> _logger;
    private static readonly List<BillingAuditLog> _entries = new();

    public BillingAuditBackgroundService(Channel<BillingAuditLog> channel, ILogger<BillingAuditBackgroundService> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                _entries.Add(item);
                // TODO: Persist to durable store
                _logger.AuditEntryStored(item.EventType, item.SecretSource.ToString(), item.TraceId);
            }
            catch (Exception ex)
            {
                _logger.AuditWriteError(ex);
            }
        }
    }
}
