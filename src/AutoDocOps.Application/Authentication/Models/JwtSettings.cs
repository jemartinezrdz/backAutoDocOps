using System.ComponentModel.DataAnnotations;

namespace AutoDocOps.Application.Authentication.Models;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required(ErrorMessage = "JWT SecretKey is required")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters for security")]
    public string SecretKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "ExpirationMinutes must be between 1 and 1440 (24 hours)")]
    public int ExpirationMinutes { get; set; } = 60;

    [Range(1, 30, ErrorMessage = "RefreshTokenExpirationDays must be between 1 and 30")]
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
