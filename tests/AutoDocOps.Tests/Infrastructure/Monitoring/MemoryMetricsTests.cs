using AutoDocOps.Infrastructure.Monitoring;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace AutoDocOps.Tests.Infrastructure.Monitoring;

public class MemoryMetricsTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<MemoryMetricsTests> _logger;
    
    public MemoryMetricsTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new TestLogger<MemoryMetricsTests>(output);
    }
    
    [Fact]
    public void GetCurrentUsage_ReturnsValidMetrics()
    {
        // Act
        var usage = MemoryMetrics.GetCurrentUsage();
        
        // Assert
        usage.Should().NotBeNull();
        usage.WorkingSet.Should().BePositive();
        usage.PrivateMemory.Should().BePositive();
        usage.VirtualMemory.Should().BePositive();
        usage.GcTotalMemory.Should().BeGreaterOrEqualTo(0);
        usage.GcAllocatedBytes.Should().BePositive();
        usage.GcHeapSize.Should().BePositive();
        usage.Gen0Collections.Should().BeGreaterOrEqualTo(0);
        usage.Gen1Collections.Should().BeGreaterOrEqualTo(0);
        usage.Gen2Collections.Should().BeGreaterOrEqualTo(0);
        usage.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        
        _output.WriteLine($"Working Set: {usage.WorkingSet / 1024 / 1024:F2} MB");
        _output.WriteLine($"GC Total Memory: {usage.GcTotalMemory / 1024 / 1024:F2} MB");
    }
    
    [Fact]
    public async Task MonitorAsync_WithSimpleOperation_ReturnsResultAndMetrics()
    {
        // Arrange
        const string expectedResult = "test result";
        const string operationName = "TestOperation";
        
        Func<Task<string>> operation = async () =>
        {
            await Task.Delay(10);
            return expectedResult;
        };
        
        // Act
        var (result, delta) = await MemoryMetrics.MonitorAsync(operation, operationName, _logger);
        
        // Assert
        result.Should().Be(expectedResult);
        delta.Should().NotBeNull();
        delta.OperationName.Should().Be(operationName);
        delta.Duration.Should().BePositive();
        delta.StartTime.Should().BeBefore(delta.EndTime);
        
        _output.WriteLine($"Operation took: {delta.Duration.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Memory delta: {delta}");
    }
    
    [Fact]
    public async Task MonitorAsync_WithMemoryAllocatingOperation_DetectsMemoryUsage()
    {
        // Arrange
        const string operationName = "MemoryAllocatingOperation";
        
        Func<Task<byte[]>> operation = async () =>
        {
            await Task.Delay(1);
            // Allocate some memory
            var data = new byte[1024 * 1024]; // 1MB
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(i % 256);
            }
            return data;
        };
        
        // Act
        var (result, delta) = await MemoryMetrics.MonitorAsync(operation, operationName, _logger);
        
        // Assert
        result.Should().NotBeNull();
        result.Length.Should().Be(1024 * 1024);
        delta.AllocatedBytesDelta.Should().BePositive();
        
        _output.WriteLine($"Allocated bytes delta: {delta.AllocatedBytesDelta:N0} bytes");
    }
    
    [Fact]
    public async Task MonitorAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            MemoryMetrics.MonitorAsync<string>(null!, "test"));
    }
    
    [Fact]
    public async Task MonitorAsync_WithEmptyOperationName_ThrowsArgumentException()
    {
        // Arrange
        Func<Task<string>> operation = () => Task.FromResult("test");
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            MemoryMetrics.MonitorAsync(operation, ""));
    }
    
    [Fact]
    public void CheckMemoryPressure_WithDefaultThresholds_ReturnsValidInfo()
    {
        // Act
        var pressureInfo = MemoryMetrics.CheckMemoryPressure();
        
        // Assert
        pressureInfo.Should().NotBeNull();
        pressureInfo.WorkingSetMB.Should().BePositive();
        pressureInfo.GcMemoryMB.Should().BeGreaterOrEqualTo(0);
        pressureInfo.WorkingSetThresholdMB.Should().Be(1024);
        pressureInfo.GcMemoryThresholdMB.Should().Be(512);
        pressureInfo.RecommendedAction.Should().NotBeNullOrWhiteSpace();
        
        _output.WriteLine($"Working Set: {pressureInfo.WorkingSetMB} MB");
        _output.WriteLine($"GC Memory: {pressureInfo.GcMemoryMB} MB");
        _output.WriteLine($"Under Pressure: {pressureInfo.IsUnderPressure}");
        _output.WriteLine($"Recommended Action: {pressureInfo.RecommendedAction}");
    }
    
    [Fact]
    public void CheckMemoryPressure_WithLowThresholds_DetectsPressure()
    {
        // Arrange - Set very low thresholds to trigger pressure detection
        const int lowWorkingSetThreshold = 1; // 1 MB
        const int lowGcMemoryThreshold = 1; // 1 MB
        
        // Act
        var pressureInfo = MemoryMetrics.CheckMemoryPressure(lowWorkingSetThreshold, lowGcMemoryThreshold);
        
        // Assert
        pressureInfo.IsUnderPressure.Should().BeTrue();
        pressureInfo.RecommendedAction.Should().Contain("memory usage");
    }
    
    [Fact]
    public void CheckMemoryPressure_WithHighThresholds_NoPressure()
    {
        // Arrange - Set very high thresholds
        const int highWorkingSetThreshold = 100000; // 100GB
        const int highGcMemoryThreshold = 100000; // 100GB
        
        // Act
        var pressureInfo = MemoryMetrics.CheckMemoryPressure(highWorkingSetThreshold, highGcMemoryThreshold);
        
        // Assert
        pressureInfo.IsUnderPressure.Should().BeFalse();
        pressureInfo.RecommendedAction.Should().Be("Memory usage within normal limits");
    }
    
    [Fact]
    public async Task MonitorAsync_WithExceptionInOperation_PropagatesException()
    {
        // Arrange
        const string operationName = "FailingOperation";
        var expectedException = new InvalidOperationException("Test exception");
        
        Func<Task<string>> operation = () => throw expectedException;
        
        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MemoryMetrics.MonitorAsync(operation, operationName, _logger));
        
        actualException.Should().Be(expectedException);
    }
    
    [Fact]
    public void MemoryUsageDelta_ToString_ReturnsFormattedString()
    {
        // Arrange
        var delta = new MemoryUsageDelta
        {
            OperationName = "TestOp",
            Duration = TimeSpan.FromMilliseconds(123.45),
            WorkingSetDelta = 1024 * 1024, // 1MB
            GcMemoryDelta = 512 * 1024, // 512KB
            NewGen2Collections = 2,
            StartTime = DateTimeOffset.UtcNow.AddSeconds(-1),
            EndTime = DateTimeOffset.UtcNow
        };
        
        // Act
        var result = delta.ToString();
        
        // Assert
        result.Should().Contain("TestOp");
        result.Should().Contain("123.45ms");
        result.Should().Contain("1.00MB"); // Working set delta
        result.Should().Contain("0.50MB"); // GC memory delta
        result.Should().Contain("Gen2 Collections: 2");
        
        _output.WriteLine($"Delta string: {result}");
    }
    
    [Fact]
    public async Task MonitorAsync_ConcurrentOperations_HandlesCorrectly()
    {
        // Arrange
        const int operationCount = 5;
        var operations = new List<Task<(string Result, MemoryUsageDelta Delta)>>();
        
        // Act
        for (int i = 0; i < operationCount; i++)
        {
            var operationIndex = i;
            var operation = MemoryMetrics.MonitorAsync(
                async () =>
                {
                    await Task.Delay(Random.Shared.Next(10, 50));
                    return $"Result {operationIndex}";
                },
                $"Operation{operationIndex}",
                _logger);
            
            operations.Add(operation);
        }
        
        var results = await Task.WhenAll(operations);
        
        // Assert
        results.Should().HaveCount(operationCount);
        
        for (int i = 0; i < operationCount; i++)
        {
            var (result, delta) = results[i];
            result.Should().Be($"Result {i}");
            delta.OperationName.Should().Be($"Operation{i}");
            delta.Duration.Should().BePositive();
        }
    }
}

/// <summary>
/// Simple test logger that outputs to xUnit test output
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _output;
    
    public TestLogger(ITestOutputHelper output)
    {
        _output = output;
    }
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    
    public bool IsEnabled(LogLevel logLevel) => true;
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
    // Analyzer guard (CA1062)
    ArgumentNullException.ThrowIfNull(formatter);
        var message = formatter(state, exception);
        _output.WriteLine($"[{logLevel}] {message}");
        
        if (exception != null)
        {
            _output.WriteLine($"Exception: {exception}");
        }
    }
    
    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}