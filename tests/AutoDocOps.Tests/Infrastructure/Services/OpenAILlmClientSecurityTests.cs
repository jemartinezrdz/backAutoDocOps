using AutoDocOps.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security;
using Xunit;

namespace AutoDocOps.Tests.Infrastructure.Services;

/// <summary>
/// Security-focused tests for OpenAI LLM Client API key validation
/// </summary>
public class OpenAILlmClientSecurityTests
{
    private readonly Mock<ILogger<OpenAILlmClient>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public OpenAILlmClientSecurityTests()
    {
        _mockLogger = new Mock<ILogger<OpenAILlmClient>>();
        _mockConfiguration = new Mock<IConfiguration>();
    }

    [Fact]
    public void Constructor_WithNullApiKey_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["OpenAI:ApiKey"]).Returns((string?)null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new OpenAILlmClient(_mockConfiguration.Object, _mockLogger.Object));
        Assert.Contains("OpenAI API key is not configured", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyApiKey_ThrowsSecurityException(string invalidApiKey)
    {
        // Arrange
        _mockConfiguration.Setup(x => x["OpenAI:ApiKey"]).Returns(invalidApiKey);

        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => new OpenAILlmClient(_mockConfiguration.Object, _mockLogger.Object));
        Assert.Contains("OpenAI API key is invalid or malformed", exception.Message);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("sk-tooshort")]
    [InlineData("sk-proj-12345")] // Too short
    public void Constructor_WithTooShortApiKey_ThrowsSecurityException(string shortApiKey)
    {
        // Arrange
        _mockConfiguration.Setup(x => x["OpenAI:ApiKey"]).Returns(shortApiKey);
        _mockConfiguration.Setup(x => x["OpenAI:UseAzure"]).Returns("false");

        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => new OpenAILlmClient(_mockConfiguration.Object, _mockLogger.Object));
        Assert.Contains("OpenAI API key is invalid or malformed", exception.Message);
    }

    [Theory]
    [InlineData("sk-invalid_chars!@#$")]
    [InlineData("sk-proj-has spaces")]
    [InlineData("sk-proj-<script>alert('xss')</script>")]
    public void Constructor_WithInvalidCharacters_ThrowsSecurityException(string invalidApiKey)
    {
        // Arrange
        _mockConfiguration.Setup(x => x["OpenAI:ApiKey"]).Returns(invalidApiKey);
        _mockConfiguration.Setup(x => x["OpenAI:UseAzure"]).Returns("false");

        // Act & Assert
        var exception = Assert.Throws<SecurityException>(() => new OpenAILlmClient(_mockConfiguration.Object, _mockLogger.Object));
        Assert.Contains("OpenAI API key is invalid or malformed", exception.Message);
    }

    [Theory]
    [InlineData("sk-proj-1234567890abcdef1234567890abcdef1234567890abcdef12")] // 51 chars (valid OpenAI)
    [InlineData("sk-1234567890abcdef1234567890abcdef1234567890abcdef1234")] // 48 chars (valid legacy)
    public void Constructor_WithValidApiKey_DoesNotThrow(string validApiKey)
    {
        // Arrange
        _mockConfiguration.Setup(x => x["OpenAI:ApiKey"]).Returns(validApiKey);
        _mockConfiguration.Setup(x => x["OpenAI:Endpoint"]).Returns((string?)null); // Use OpenAI directly

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => new OpenAILlmClient(_mockConfiguration.Object, _mockLogger.Object));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithValidAzureSettings_DoesNotThrow()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["OpenAI:Endpoint"]).Returns("https://my-resource.openai.azure.com/");
        _mockConfiguration.Setup(x => x["OpenAI:ApiKey"]).Returns("sk-1234567890abcdef1234567890abcdef12345678"); // Valid Azure key
        _mockConfiguration.Setup(x => x["OpenAI:Model"]).Returns("gpt-35-turbo");

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => new OpenAILlmClient(_mockConfiguration.Object, _mockLogger.Object));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("http://example.com/")] // HTTP not HTTPS
    [InlineData("https://malicious-site.com/")] // Not Azure domain
    [InlineData("https://my-resource.openai.azure.com.evil.com/")] // Domain spoofing
    public void Constructor_WithInvalidAzureEndpoint_StillWorksIfApiKeyValid(string invalidEndpoint)
    {
        // Arrange - Azure endpoint validation is not implemented in current code
        _mockConfiguration.Setup(x => x["OpenAI:Endpoint"]).Returns(invalidEndpoint);
        _mockConfiguration.Setup(x => x["OpenAI:ApiKey"]).Returns("sk-1234567890abcdef1234567890abcdef12345678");

        // Act & Assert - Current implementation doesn't validate Azure endpoint
        var exception = Record.Exception(() => new OpenAILlmClient(_mockConfiguration.Object, _mockLogger.Object));
        // This might throw Azure client initialization error, but not SecurityException
        if (exception != null)
        {
            Assert.IsNotType<SecurityException>(exception);
        }
    }

    [Fact]
    public void Constructor_LogsSecurityValidation()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["OpenAI:ApiKey"]).Returns("sk-proj-1234567890abcdef1234567890abcdef1234567890abcdef12");
        _mockConfiguration.Setup(x => x["OpenAI:Endpoint"]).Returns((string?)null);

        // Act
        _ = new OpenAILlmClient(_mockConfiguration.Object, _mockLogger.Object);

        // Assert - Check if warning was logged for invalid format (there shouldn't be one for valid key)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Invalid OpenAI API key format")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
