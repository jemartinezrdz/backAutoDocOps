using System.Text.RegularExpressions;

namespace AutoDocOps.Infrastructure.Validators;

/// <summary>
/// Validates OpenAI API keys with comprehensive security checks
/// </summary>
public static class ApiKeyValidator
{
    private static readonly string[] ValidPrefixes = { "sk-", "sk-proj-", "sk-org-" };
    private static readonly Regex ApiKeyFormat = 
        new(@"^(sk|sk-proj|sk-org)-[a-zA-Z0-9]{16,}$", RegexOptions.Compiled);
    
    private const int MinApiKeyLength = 20;

    /// <summary>
    /// Validates OpenAI API key format and structure
    /// </summary>
    /// <param name="key">The API key to validate</param>
    /// <returns>True if the key is valid, false otherwise</returns>
    public static bool IsValidOpenAiKey(string? key) =>
        key is { Length: >= MinApiKeyLength } && 
        ValidPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal)) &&
        ApiKeyFormat.IsMatch(key);

    /// <summary>
    /// Validates OpenAI API key and returns detailed validation result
    /// </summary>
    /// <param name="key">The API key to validate</param>
    /// <returns>Validation result with specific failure reason</returns>
    public static ApiKeyValidationResult ValidateWithDetails(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return new ApiKeyValidationResult(false, "API key is null or empty");

        if (key.Length < MinApiKeyLength)
            return new ApiKeyValidationResult(false, $"API key is too short (minimum {MinApiKeyLength} characters)");

        if (!ValidPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal)))
            return new ApiKeyValidationResult(false, $"API key must start with one of: {string.Join(", ", ValidPrefixes)}");

        if (!ApiKeyFormat.IsMatch(key))
            return new ApiKeyValidationResult(false, "API key contains invalid characters or format");

        return new ApiKeyValidationResult(true, "Valid API key");
    }
}

/// <summary>
/// Result of API key validation with detailed feedback
/// </summary>
public record ApiKeyValidationResult(bool IsValid, string Message);
