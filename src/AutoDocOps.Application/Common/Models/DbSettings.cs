using System.ComponentModel.DataAnnotations;

namespace AutoDocOps.Application.Common.Models;

public class DbSettings
{
    public const string SectionName = "ConnectionStrings";

    [Required(ErrorMessage = "Database connection string is required")]
    [MinLength(10, ErrorMessage = "Database connection string must be at least 10 characters")]
    public string DefaultConnection { get; set; } = string.Empty;

    [Required(ErrorMessage = "Redis connection string is required")]
    [MinLength(5, ErrorMessage = "Redis connection string must be at least 5 characters")]
    public string Redis { get; set; } = string.Empty;
}
