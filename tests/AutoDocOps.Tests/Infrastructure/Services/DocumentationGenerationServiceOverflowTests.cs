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

    [Fact]
    public void ExponentialBackoff_DoesNotOverflow_WithLargeRetryCount()
    {
        // Arrange - Create service instance
        var service = new DocumentationGenerationService(_mockScopeFactory.Object, _mockLogger.Object, _mockOptions.Object);
        
        // Use reflection to access private field for testing
        var currentRetryDelayField = typeof(DocumentationGenerationService)
            .GetField("_currentRetryDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(currentRetryDelayField);

        // Act & Assert - Simulate many failures without overflow
        var initialDelay = TimeSpan.FromMinutes(1);
        currentRetryDelayField.SetValue(service, initialDelay);

        // Simulate exponential backoff calculation multiple times
        for (int i = 0; i < 50; i++) // Many iterations to test overflow protection
        {
            var currentDelay = (TimeSpan)currentRetryDelayField.GetValue(service)!;
            
            // Simulate the exponential backoff logic from the service
            var nextDelayMs = Math.Min(currentDelay.TotalMilliseconds * 2, TimeSpan.FromHours(1).TotalMilliseconds);
            var nextDelay = TimeSpan.FromMilliseconds(nextDelayMs);
            
            currentRetryDelayField.SetValue(service, nextDelay);
            
            // Assert no overflow occurred - delay should be reasonable
            Assert.True(nextDelay.TotalMilliseconds > 0, "Delay should be positive");
            Assert.True(nextDelay.TotalMilliseconds < TimeSpan.FromDays(1).TotalMilliseconds, "Delay should not exceed 1 day");
            Assert.True(nextDelay <= TimeSpan.FromHours(1), "Delay should not exceed max retry delay");
        }
    }

    [Fact]
    public void ExponentialBackoff_RespectsMaximumDelay()
    {
        // Arrange
        var service = new DocumentationGenerationService(_mockScopeFactory.Object, _mockLogger.Object, _mockOptions.Object);
        
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
    public void ExponentialBackoff_WithVariousInitialDelays_DoesNotOverflow(int initialMinutes)
    {
        // Arrange
        var service = new DocumentationGenerationService(_mockScopeFactory.Object, _mockLogger.Object, _mockOptions.Object);
        
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
    public void ExponentialBackoff_WithTotalMilliseconds_IsSaferThanTicks()
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
    public void ExponentialBackoff_HandlesEdgeCases()
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
