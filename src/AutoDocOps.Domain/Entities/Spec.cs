using System.ComponentModel.DataAnnotations;

namespace AutoDocOps.Domain.Entities;

public class Spec
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid ProjectId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Language { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string FileType { get; set; } = string.Empty;
    
    public int LineCount { get; set; }
    
    public long SizeInBytes { get; set; }
    
    public string? ParsedMetadata { get; set; } // JSON string
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Project Project { get; set; } = null!;
}

