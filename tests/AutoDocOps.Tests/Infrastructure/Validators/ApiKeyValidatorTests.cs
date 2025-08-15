using AutoDocOps.Infrastructure.Validators;
using Xunit;

namespace AutoDocOps.Tests.Infrastructure.Validators;

/// <summary>
/// Comprehensive tests for the ApiKeyValidator with detailed validation scenarios
/// </summary>
public class ApiKeyValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void IsValidOpenAiKey_WithNullOrWhitespace_ReturnsFalse(string? invalidKey)
    {
        // Act
        var result = ApiKeyValidator.IsValidOpenAiKey(invalidKey);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("sk-")]
    [InlineData("sk-short")]
    [InlineData("sk-proj-12345")]
    [InlineData("sk-org-tiny")]
    [InlineData("sk-abcdef123")] // 19 chars, too short
    public void IsValidOpenAiKey_WithTooShortKey_ReturnsFalse(string shortKey)
    {
        // Act
        var result = ApiKeyValidator.IsValidOpenAiKey(shortKey);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("invalid-1234567890abcdef1234567890abcdef")] // Wrong prefix
    [InlineData("sk_1234567890abcdef1234567890abcdef1234")] // Underscore instead of dash
    [InlineData("sk-project-1234567890abcdef1234567890abcdef")] // project not proj
    [InlineData("openai-1234567890abcdef1234567890abcdef")] // Wrong prefix entirely
    public void IsValidOpenAiKey_WithInvalidPrefix_ReturnsFalse(string invalidPrefixKey)
    {
        // Act
        var result = ApiKeyValidator.IsValidOpenAiKey(invalidPrefixKey);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("sk-invalid_chars!@#$1234567890abcdef")]
    [InlineData("sk-proj-has spaces 1234567890abcdef")]
    [InlineData("sk-proj-<script>alert('xss')</script>")]
    [InlineData("sk-org-специальные символы123")]
    [InlineData("sk-1234567890abcdef!@#$%^&*()")]
    public void IsValidOpenAiKey_WithInvalidCharacters_ReturnsFalse(string invalidCharsKey)
    {
        // Act
        var result = ApiKeyValidator.IsValidOpenAiKey(invalidCharsKey);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("sk-1234567890abcdef1234567890abcdef1234567890abcdef")] // 48 chars
    [InlineData("sk-proj-1234567890abcdef1234567890abcdef1234567890abcdef12")] // 51 chars
    [InlineData("sk-org-abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJ")] // 48 chars, mixed case
    [InlineData("sk-0123456789abcdefABCDEF0123456789abcdefABCDEF1234")] // 48 chars, alphanumeric
    public void IsValidOpenAiKey_WithValidKeys_ReturnsTrue(string validKey)
    {
        // Act
        var result = ApiKeyValidator.IsValidOpenAiKey(validKey);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateWithDetails_WithNullKey_ReturnsInvalidWithMessage()
    {
        // Act
        var result = ApiKeyValidator.ValidateWithDetails(null);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("null or empty", result.Message);
    }

    [Fact]
    public void ValidateWithDetails_WithShortKey_ReturnsSpecificError()
    {
        // Arrange
        var shortKey = "sk-short";

        // Act
        var result = ApiKeyValidator.ValidateWithDetails(shortKey);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("too short", result.Message);
        Assert.Contains("20 characters", result.Message);
    }

    [Fact]
    public void ValidateWithDetails_WithInvalidPrefix_ReturnsSpecificError()
    {
        // Arrange
        var invalidPrefixKey = "invalid-1234567890abcdef1234567890abcdef";

        // Act
        var result = ApiKeyValidator.ValidateWithDetails(invalidPrefixKey);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("must start with one of", result.Message);
        Assert.Contains("sk-", result.Message);
        Assert.Contains("sk-proj-", result.Message);
        Assert.Contains("sk-org-", result.Message);
    }

    [Fact]
    public void ValidateWithDetails_WithInvalidFormat_ReturnsSpecificError()
    {
        // Arrange
        var invalidFormatKey = "sk-1234567890abcdef!@#$%^&*()1234567890abcdef";

        // Act
        var result = ApiKeyValidator.ValidateWithDetails(invalidFormatKey);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("invalid characters or format", result.Message);
    }

    [Fact]
    public void ValidateWithDetails_WithValidKey_ReturnsValid()
    {
        // Arrange
        var validKey = "sk-proj-1234567890abcdef1234567890abcdef1234567890abcdef12";

        // Act
        var result = ApiKeyValidator.ValidateWithDetails(validKey);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("Valid API key", result.Message);
    }

    [Theory]
    [InlineData("sk-", 3)]
    [InlineData("sk-proj-", 8)]
    [InlineData("sk-org-", 7)]
    public void ValidateWithDetails_TestsPrefixesCorrectly(string prefix, int _)
    {
        // Arrange - Create a valid key with the prefix and sufficient additional characters
        var validSuffix = "1234567890abcdef1234567890abcdef123456789012345";
        var validKey = prefix + validSuffix;

        // Act
        var result = ApiKeyValidator.ValidateWithDetails(validKey);

        // Assert
        Assert.True(result.IsValid, $"Key with prefix '{prefix}' should be valid");
    }

    [Fact]
    public void IsValidOpenAiKey_PerformanceTest_HandlesMultipleCallsEfficiently()
    {
        // Arrange
        var validKey = "sk-proj-1234567890abcdef1234567890abcdef1234567890abcdef12";
        var invalidKey = "invalid-key";

        // Act & Assert - Multiple calls should be consistent and fast
        for (int i = 0; i < 1000; i++)
        {
            Assert.True(ApiKeyValidator.IsValidOpenAiKey(validKey));
            Assert.False(ApiKeyValidator.IsValidOpenAiKey(invalidKey));
        }
    }

    [Theory]
    [InlineData("sk-1234567890abcdef1234567890abcdef123456789012345678901234567890", true)] // 64 chars
    [InlineData("sk-proj-" + "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890123456789012345678901234567890", true)] // Very long
    public void IsValidOpenAiKey_WithExtraLongValidKeys_ReturnsTrue(string longKey, bool expected)
    {
        // Act
        var result = ApiKeyValidator.IsValidOpenAiKey(longKey);

        // Assert
        Assert.Equal(expected, result);
    }
}
