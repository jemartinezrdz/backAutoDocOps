using System.ComponentModel.DataAnnotations;

namespace AutoDocOps.Domain.Entities;

public class Passport
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid ProjectId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Version { get; set; } = string.Empty;
    
    [Required]
    public string DocumentationContent { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Format { get; set; } = "markdown"; // markdown, html, pdf
    
    public string? Metadata { get; set; } // JSON string with generation metadata
    
    public PassportStatus Status { get; set; } = PassportStatus.Generating;
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    [Required]
    public Guid GeneratedBy { get; set; }
    
    public long SizeInBytes { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    public virtual Project Project { get; set; } = null!;
}

public enum PassportStatus
{
    Generating = 0,
    Completed = 1,
    Failed = 2,
    Cancelled = 3
}

