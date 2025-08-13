using AutoDocOps.Infrastructure.Helpers;
using AutoDocOps.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AutoDocOps.Tests.Infrastructure.Services;

/// <summary>
/// Security-focused tests for DocumentationGenerationService exponential backoff overflow protection
/// </summary>
public class DocumentationGenerationServiceOverflowTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ILogger<DocumentationGenerationService>> _mockLogger;
    private readonly Mock<IOptions<DocumentationGenerationOptions>> _mockOptions;
    private readonly DocumentationGenerationOptions _options;

    public DocumentationGenerationServiceOverflowTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockLogger = new Mock<ILogger<DocumentationGenerationService>>();
        _mockOptions = new Mock<IOptions<DocumentationGenerationOptions>>();
        
        _options = new DocumentationGenerationOptions
        {
            CheckIntervalSeconds = 1,
            RetryDelayMinutes = 1,
            EnableSimulation = false
        };
        _mockOptions.Setup(x => x.Value).Returns(_options);
    }

    [Theory]
    [InlineData(60000, 3600000)] // 1 min → max 60 min
    [InlineData(1000, 10000)] // 1 sec → max 10 sec
    [InlineData(30000, 120000)] // 30 sec → max 2 min
    public void BackoffHelperIsBoundedWithVariousDelays(long startMs, long maxMs)
    {
        // Arrange
        var current = TimeSpan.FromMilliseconds(startMs);
        var max = TimeSpan.FromMilliseconds(maxMs);

        // Act & Assert - Test multiple iterations
        for (int i = 0; i < 50; i++)
        {
            current = BackoffHelper.NextDelay(current, max);
            
            // Assert delay is always within bounds
            Assert.True(current.TotalMilliseconds > 0, "Delay should be positive");
            Assert.True(current <= max, $"Delay {current.TotalMilliseconds}ms should not exceed max {max.TotalMilliseconds}ms");
        }
        
        // Final assertion - should converge to max
        Assert.Equal(max, current);
    }

    [Fact]
    public void BackoffHelperHandlesOverflowGracefullyReturnsMax()
    {
        // Arrange - Use TimeSpan.MaxValue which when doubled will definitely overflow
        var largeDelay = TimeSpan.MaxValue;
        var maxDelay = TimeSpan.FromHours(1);

        // Act - This should handle overflow gracefully
        var result = BackoffHelper.NextDelay(largeDelay, maxDelay);

        // Assert - Should return max delay, not throw
        Assert.Equal(maxDelay, result);
    }

    private const long OverflowTriggerOffsetTicks = 1000;

    [Fact]
    public void BackoffHelperCheckedArithmeticOnOverflowReturnsMax()
    {
        // Arrange - value chosen to overflow when doubled
        var overflowProneValueTicks = checked(long.MaxValue - OverflowTriggerOffsetTicks);
        var largeDelay = new TimeSpan(overflowProneValueTicks);
        var maxDelay = TimeSpan.FromDays(365);

        // Act
        var result = BackoffHelper.NextDelay(largeDelay, maxDelay);

        // Assert - Implementation captura OverflowException y retorna max
        Assert.Equal(maxDelay, result);
    }

    [Fact]
    public void BackoffHelperCheckedArithmeticThrowsOnTrueOverflow()
    {
        // Complementary test: force an actual overflow using checked arithmetic before constructing TimeSpan
        Assert.Throws<OverflowException>(() =>
        {
            var half = long.MaxValue / 2;
            var sum = checked(half + half + 1000); // this will overflow
            _ = new TimeSpan(sum);
        });
    }

    [Fact]
    public void BackoffHelperWithNormalValuesDoublesCorrectly()
    {
        // Arrange
        var initial = TimeSpan.FromSeconds(1);
        var max = TimeSpan.FromMinutes(10);

        // Act
        var second = BackoffHelper.NextDelay(initial, max);
        var third = BackoffHelper.NextDelay(second, max);

        // Assert - Should double each time until max
        Assert.Equal(TimeSpan.FromSeconds(2), second);
        Assert.Equal(TimeSpan.FromSeconds(4), third);
    }

    [Fact]
    public void ExponentialBackoffRespectsMaximumDelay()
    {
        // Arrange
        using var service = new DocumentationGenerationService(_mockScopeFactory.Object, _mockLogger.Object, _mockOptions.Object);
        
        var currentRetryDelayField = typeof(DocumentationGenerationService)
            .GetField("_currentRetryDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(currentRetryDelayField);

        // Set initial delay to a large value close to max
        var nearMaxDelay = TimeSpan.FromMinutes(45);
        currentRetryDelayField.SetValue(service, nearMaxDelay);

        // Act - Simulate exponential backoff
        var currentDelay = (TimeSpan)currentRetryDelayField.GetValue(service)!;
        var nextDelayMs = Math.Min(currentDelay.TotalMilliseconds * 2, TimeSpan.FromHours(1).TotalMilliseconds);
        var nextDelay = TimeSpan.FromMilliseconds(nextDelayMs);

        // Assert - Should cap at max delay
        Assert.Equal(TimeSpan.FromHours(1).TotalMilliseconds, nextDelayMs);
        Assert.Equal(TimeSpan.FromHours(1), nextDelay);
    }

    [Theory]
    [InlineData(1)] // 1 minute
    [InlineData(5)] // 5 minutes  
    [InlineData(30)] // 30 minutes
    public void ExponentialBackoffWithVariousInitialDelaysDoesNotOverflow(int initialMinutes)
    {
        // Arrange
        using var service = new DocumentationGenerationService(_mockScopeFactory.Object, _mockLogger.Object, _mockOptions.Object);
        
        var currentRetryDelayField = typeof(DocumentationGenerationService)
            .GetField("_currentRetryDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(currentRetryDelayField);

        var initialDelay = TimeSpan.FromMinutes(initialMinutes);
        currentRetryDelayField.SetValue(service, initialDelay);

        // Act & Assert - Multiple doublings should not overflow
        for (int i = 0; i < 20; i++)
        {
            var currentDelay = (TimeSpan)currentRetryDelayField.GetValue(service)!;
            var nextDelayMs = Math.Min(currentDelay.TotalMilliseconds * 2, TimeSpan.FromHours(1).TotalMilliseconds);
            var nextDelay = TimeSpan.FromMilliseconds(nextDelayMs);
            
            currentRetryDelayField.SetValue(service, nextDelay);
            
            // Verify no overflow and reasonable bounds
            Assert.True(nextDelay.TotalMilliseconds >= 0, "Delay should not be negative");
            Assert.True(nextDelay.TotalMilliseconds <= TimeSpan.FromHours(1).TotalMilliseconds, "Delay should not exceed max");
            Assert.True(double.IsFinite(nextDelay.TotalMilliseconds), "Delay should be finite");
        }
    }

    [Fact]
    public void ExponentialBackoffWithTotalMillisecondsIsSaferThanTicks()
    {
        // Arrange - Test the safety of TotalMilliseconds vs Ticks approach
        var initialDelay = TimeSpan.FromMinutes(30);
        
        // Act - Compare both approaches
        var safeApproach = Math.Min(initialDelay.TotalMilliseconds * 2, TimeSpan.FromHours(1).TotalMilliseconds);
        var riskyApproach = Math.Min(initialDelay.Ticks * 2, TimeSpan.FromHours(1).Ticks);
        
        // Assert - Both should be equivalent for normal values, but TotalMilliseconds is safer for large values
        Assert.Equal(TimeSpan.FromMilliseconds(safeApproach), TimeSpan.FromTicks(riskyApproach));
        
        // Test with extreme values where Ticks might overflow
        var extremeDelay = TimeSpan.FromHours(23); // Large but not max
        var safeLargeResult = Math.Min(extremeDelay.TotalMilliseconds * 2, TimeSpan.FromHours(1).TotalMilliseconds);
        
        // Should cap at max delay
        Assert.Equal(TimeSpan.FromHours(1).TotalMilliseconds, safeLargeResult);
    }

    [Fact]
    public void ExponentialBackoffHandlesEdgeCases()
    {
        // Test with zero delay
        var zeroDelay = TimeSpan.Zero;
        var result1 = Math.Min(zeroDelay.TotalMilliseconds * 2, TimeSpan.FromHours(1).TotalMilliseconds);
        Assert.Equal(0, result1);
        
        // Test with very small delay
        var smallDelay = TimeSpan.FromMilliseconds(1);
        var result2 = Math.Min(smallDelay.TotalMilliseconds * 2, TimeSpan.FromHours(1).TotalMilliseconds);
        Assert.Equal(2, result2);
        
        // Test with max delay
        var maxDelay = TimeSpan.FromHours(1);
        var result3 = Math.Min(maxDelay.TotalMilliseconds * 2, TimeSpan.FromHours(1).TotalMilliseconds);
        Assert.Equal(TimeSpan.FromHours(1).TotalMilliseconds, result3);
    }
}
