using System.Diagnostics;
using System.Runtime;
using Microsoft.Extensions.Logging;
using AutoDocOps.Infrastructure.Logging;

namespace AutoDocOps.Infrastructure.Monitoring;

/// <summary>
/// Provides memory usage metrics and monitoring capabilities
/// </summary>
public class MemoryMetrics
{
    private static readonly ActivitySource ActivitySource = new("AutoDocOps.Memory");
    
    /// <summary>
    /// Gets current memory usage statistics
    /// </summary>
    public static MemoryUsageInfo GetCurrentUsage()
    {
        using var activity = ActivitySource.StartActivity("GetMemoryUsage");
        
        var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();
        
        return new MemoryUsageInfo
        {
            WorkingSet = process.WorkingSet64,
            PrivateMemory = process.PrivateMemorySize64,
            VirtualMemory = process.VirtualMemorySize64,
            GcTotalMemory = GC.GetTotalMemory(forceFullCollection: false),
            GcAllocatedBytes = GC.GetTotalAllocatedBytes(),
            GcHeapSize = gcInfo.HeapSizeBytes,
            GcFragmentedBytes = gcInfo.FragmentedBytes,
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            Timestamp = DateTimeOffset.UtcNow
        };
    }
    
    /// <summary>
    /// Monitors memory usage during an operation
    /// </summary>
    /// <typeparam name="T">Return type of the operation</typeparam>
    /// <param name="operation">The operation to monitor</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="logger">Logger instance</param>
    /// <returns>Tuple of operation result and memory metrics</returns>
    public static async Task<(T Result, MemoryUsageDelta Delta)> MonitorAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        
        using var activity = ActivitySource.StartActivity($"Monitor_{operationName}");
        
        var before = GetCurrentUsage();
    logger?.MemoryMonitorStart(operationName);
        
        var stopwatch = Stopwatch.StartNew();
    var result = await operation().ConfigureAwait(false);
        stopwatch.Stop();
        
        var after = GetCurrentUsage();
        
        var delta = new MemoryUsageDelta
        {
            OperationName = operationName,
            Duration = stopwatch.Elapsed,
            WorkingSetDelta = after.WorkingSet - before.WorkingSet,
            PrivateMemoryDelta = after.PrivateMemory - before.PrivateMemory,
            GcMemoryDelta = after.GcTotalMemory - before.GcTotalMemory,
            AllocatedBytesDelta = after.GcAllocatedBytes - before.GcAllocatedBytes,
            NewGen0Collections = after.Gen0Collections - before.Gen0Collections,
            NewGen1Collections = after.Gen1Collections - before.Gen1Collections,
            NewGen2Collections = after.Gen2Collections - before.Gen2Collections,
            StartTime = before.Timestamp,
            EndTime = after.Timestamp
        };
        
        // Log memory pressure warnings
        if (delta.WorkingSetDelta > 50 * 1024 * 1024) // 50MB increase
        {
            logger?.MemoryHighUsage(operationName, delta.WorkingSetDelta / 1024.0 / 1024.0);
        }
        
        if (delta.NewGen2Collections > 0)
        {
            logger?.MemoryGen2Collections(operationName, delta.NewGen2Collections);
        }
        
        // Add telemetry tags
        activity?.SetTag("memory.working_set_delta", delta.WorkingSetDelta);
        activity?.SetTag("memory.gc_collections_gen2", delta.NewGen2Collections);
        activity?.SetTag("operation.duration_ms", delta.Duration.TotalMilliseconds);
        
    logger?.MemoryMonitorCompleted(operationName, delta.ToString());
        
        return (result, delta);
    }
    
    /// <summary>
    /// Checks if current memory usage exceeds thresholds
    /// </summary>
    /// <param name="workingSetThresholdMB">Working set threshold in MB</param>
    /// <param name="gcMemoryThresholdMB">GC memory threshold in MB</param>
    /// <returns>Memory pressure information</returns>
    public static MemoryPressureInfo CheckMemoryPressure(
        long workingSetThresholdMB = 1024,
        long gcMemoryThresholdMB = 512)
    {
        var usage = GetCurrentUsage();
        var workingSetMB = usage.WorkingSet / 1024 / 1024;
        var gcMemoryMB = usage.GcTotalMemory / 1024 / 1024;
        
        return new MemoryPressureInfo
        {
            IsUnderPressure = workingSetMB > workingSetThresholdMB || gcMemoryMB > gcMemoryThresholdMB,
            WorkingSetMB = workingSetMB,
            GcMemoryMB = gcMemoryMB,
            WorkingSetThresholdMB = workingSetThresholdMB,
            GcMemoryThresholdMB = gcMemoryThresholdMB,
            RecommendedAction = GetRecommendedAction(workingSetMB, gcMemoryMB, workingSetThresholdMB, gcMemoryThresholdMB)
        };
    }
    
    private static string GetRecommendedAction(long workingSetMB, long gcMemoryMB, long workingSetThreshold, long gcMemoryThreshold)
    {
        if (workingSetMB > workingSetThreshold * 2 || gcMemoryMB > gcMemoryThreshold * 2)
        {
            return "Critical memory usage - consider immediate garbage collection and resource cleanup";
        }
        
        if (workingSetMB > workingSetThreshold || gcMemoryMB > gcMemoryThreshold)
        {
            return "High memory usage - monitor closely and consider optimization";
        }
        
        return "Memory usage within normal limits";
    }
}

/// <summary>
/// Current memory usage information
/// </summary>
public record MemoryUsageInfo
{
    public long WorkingSet { get; init; }
    public long PrivateMemory { get; init; }
    public long VirtualMemory { get; init; }
    public long GcTotalMemory { get; init; }
    public long GcAllocatedBytes { get; init; }
    public long GcHeapSize { get; init; }
    public long GcFragmentedBytes { get; init; }
    public int Gen0Collections { get; init; }
    public int Gen1Collections { get; init; }
    public int Gen2Collections { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Memory usage change between two points in time
/// </summary>
public record MemoryUsageDelta
{
    public required string OperationName { get; init; }
    public TimeSpan Duration { get; init; }
    public long WorkingSetDelta { get; init; }
    public long PrivateMemoryDelta { get; init; }
    public long GcMemoryDelta { get; init; }
    public long AllocatedBytesDelta { get; init; }
    public int NewGen0Collections { get; init; }
    public int NewGen1Collections { get; init; }
    public int NewGen2Collections { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    
    public override string ToString()
    {
        return $"Operation: {OperationName}, Duration: {Duration.TotalMilliseconds:F2}ms, " +
               $"Working Set Δ: {WorkingSetDelta / 1024.0 / 1024.0:F2}MB, " +
               $"GC Memory Δ: {GcMemoryDelta / 1024.0 / 1024.0:F2}MB, " +
               $"Gen2 Collections: {NewGen2Collections}";
    }
}

/// <summary>
/// Memory pressure assessment information
/// </summary>
public record MemoryPressureInfo
{
    public bool IsUnderPressure { get; init; }
    public long WorkingSetMB { get; init; }
    public long GcMemoryMB { get; init; }
    public long WorkingSetThresholdMB { get; init; }
    public long GcMemoryThresholdMB { get; init; }
    public required string RecommendedAction { get; init; }
}