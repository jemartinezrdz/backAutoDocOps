using System.ComponentModel.DataAnnotations;

namespace AutoDocOps.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(500)]
    public string RepositoryUrl { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Branch { get; set; } = "main";
    
    [Required]
    public Guid OrganizationId { get; set; }
    
    [Required]
    public Guid CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual ICollection<Spec> Specs { get; set; } = new List<Spec>();
    public virtual ICollection<Passport> Passports { get; set; } = new List<Passport>();
}

