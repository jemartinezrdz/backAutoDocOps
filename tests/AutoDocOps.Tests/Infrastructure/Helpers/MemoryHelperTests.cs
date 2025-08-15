using AutoDocOps.Infrastructure.Helpers;
using FluentAssertions;
using System.Text;
using Xunit;

namespace AutoDocOps.Tests.Infrastructure.Helpers;

public class MemoryHelperTests
{
    [Fact]
    public async Task ReadStreamToStringAsync_WithValidStream_ReturnsCorrectString()
    {
        // Arrange
        const string expectedContent = "Hello, World! This is a test string.";
        var bytes = Encoding.UTF8.GetBytes(expectedContent);
        using var stream = new MemoryStream(bytes);
        
        // Act
        var result = await MemoryHelper.ReadStreamToStringAsync(stream, bytes.Length * 2);
        
        // Assert
        result.Should().Be(expectedContent);
    }
    
    [Fact]
    public async Task ReadStreamToStringAsync_WithMaxBytesLimit_RespectsLimit()
    {
        // Arrange
        const string content = "This is a longer string that should be truncated";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        const int maxBytes = 10;
        
        // Act
        var result = await MemoryHelper.ReadStreamToStringAsync(stream, maxBytes);
        
        // Assert
        result.Length.Should().BeLessOrEqualTo(maxBytes);
        result.Should().Be(content[..10]); // First 10 characters
    }
    
    [Fact]
    public async Task ReadStreamToStringAsync_WithNullStream_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            MemoryHelper.ReadStreamToStringAsync(null!, 100));
    }
    
    [Fact]
    public async Task ReadStreamToStringAsync_WithZeroMaxBytes_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var stream = new MemoryStream();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => 
            MemoryHelper.ReadStreamToStringAsync(stream, 0));
    }
    
    [Fact]
    public async Task ReadStreamWithLimitAsync_ReturnsCorrectData()
    {
        // Arrange
        const string content = "Test content for stream reading with limits";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        const int maxBytes = 20;
        
        // Act
        var (data, bytesRead, hasMoreData) = await MemoryHelper.ReadStreamWithLimitAsync(stream, maxBytes);
        
        // Assert - The method should read at most maxBytes
        bytesRead.Should().BeLessOrEqualTo(maxBytes, "should not read more than the limit");
        data.Length.Should().BeLessOrEqualTo(maxBytes, "returned data should not exceed limit");
        hasMoreData.Should().Be(bytes.Length > maxBytes, "hasMoreData should indicate if stream has more data");
        
        // Verify we got the expected prefix of the content
        if (bytesRead == maxBytes)
        {
            data.Should().Be(content[..maxBytes], "should return first maxBytes characters");
        }
        else
        {
            data.Should().Be(content, "should return all content if less than maxBytes");
        }
    }
    
    [Fact]
    public async Task ReadStreamWithLimitAsync_WithExactLimit_ReturnsAllData()
    {
        // Arrange
        const string content = "Exact";
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new MemoryStream(bytes);
        const int maxBytes = 5;
        
        // Act
        var (data, bytesRead, hasMoreData) = await MemoryHelper.ReadStreamWithLimitAsync(stream, maxBytes);
        
        // Assert
        data.Should().Be(content);
        bytesRead.Should().Be(5);
        hasMoreData.Should().BeFalse();
    }
    
    [Fact]
    public async Task CopyStreamAsync_CopiesDataCorrectly()
    {
        // Arrange
        const string content = "Data to be copied between streams";
        var sourceBytes = Encoding.UTF8.GetBytes(content);
        using var sourceStream = new MemoryStream(sourceBytes);
        using var destinationStream = new MemoryStream();
        
        // Act
        var copiedBytes = await MemoryHelper.CopyStreamAsync(sourceStream, destinationStream);
        
        // Assert
        copiedBytes.Should().Be(sourceBytes.Length);
        var copiedData = Encoding.UTF8.GetString(destinationStream.ToArray());
        copiedData.Should().Be(content);
    }
    
    [Fact]
    public async Task CopyStreamAsync_WithMaxBytesLimit_RespectsLimit()
    {
        // Arrange
        const string content = "This is a longer content that should be limited";
        var sourceBytes = Encoding.UTF8.GetBytes(content);
        using var sourceStream = new MemoryStream(sourceBytes);
        using var destinationStream = new MemoryStream();
        const int maxBytes = 15;
        
        // Act
        var copiedBytes = await MemoryHelper.CopyStreamAsync(sourceStream, destinationStream, maxBytes);
        
        // Assert
        copiedBytes.Should().Be(maxBytes);
        destinationStream.Length.Should().Be(maxBytes);
    }
    
    [Fact]
    public async Task CopyStreamAsync_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        using var destinationStream = new MemoryStream();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            MemoryHelper.CopyStreamAsync(null!, destinationStream));
    }
    
    [Fact]
    public async Task CopyStreamAsync_WithNullDestination_ThrowsArgumentNullException()
    {
        // Arrange
        using var sourceStream = new MemoryStream();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            MemoryHelper.CopyStreamAsync(sourceStream, null!));
    }
    
    [Theory]
    [InlineData("Short string", "Short string")]
    [InlineData("A longer string with {0} parameters {1}", "A longer string with test parameters 42")]
    public void FormatLarge_WithVariousInputs_FormatsCorrectly(string format, string expected)
    {
    ArgumentNullException.ThrowIfNull(format);
        // Arrange
        var args = format.Contains("{0}") ? new object[] { "test", 42 } : Array.Empty<object>();
        
        // Act
        var result = MemoryHelper.FormatLarge(format, args);
        
        // Assert
        result.Should().Be(expected);
    }
    
    [Fact]
    public void FormatLarge_WithNullFormat_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => MemoryHelper.FormatLarge(null!, "test"));
    }
    
    [Fact]
    public void FormatLarge_WithLargeString_UsesStringBuilder()
    {
        // Arrange
        var format = new string('x', 2000) + " {0} " + new string('y', 2000);
        var args = new object[] { "test" };
        
        // Act
        var result = MemoryHelper.FormatLarge(format, args);
        
        // Assert
        result.Should().Contain("test");
        result.Length.Should().BeGreaterThan(4000);
    }
    
    [Fact]
    public async Task ReadStreamToStringAsync_WithCancellation_RespectsCancellationToken()
    {
        // Arrange
        var content = new string('a', 10000);
        var bytes = Encoding.UTF8.GetBytes(content);
        using var stream = new SlowMemoryStream(bytes, delayMs: 100);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        
        // Act & Assert - Accept both OperationCanceledException and its derived TaskCanceledException
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            MemoryHelper.ReadStreamToStringAsync(stream, bytes.Length, cts.Token));
            
        exception.Should().NotBeNull("cancellation should be respected");
    }
}

/// <summary>
/// Helper class to simulate slow stream operations for testing cancellation
/// </summary>
public class SlowMemoryStream : MemoryStream
{
    private readonly int _delayMs;
    
    public SlowMemoryStream(byte[] buffer, int delayMs) : base(buffer)
    {
        _delayMs = delayMs;
    }
    
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delayMs, cancellationToken);
        return await base.ReadAsync(buffer, cancellationToken);
    }
}