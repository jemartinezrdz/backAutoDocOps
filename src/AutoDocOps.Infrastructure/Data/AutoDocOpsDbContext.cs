using AutoDocOps.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoDocOps.Infrastructure.Data;

public class AutoDocOpsDbContext : DbContext
{
    public AutoDocOpsDbContext(DbContextOptions<AutoDocOpsDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<Spec> Specs { get; set; }
    public DbSet<Passport> Passports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Project entity
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.RepositoryUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Branch).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OrganizationId).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();

            // Indexes
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => new { e.OrganizationId, e.IsActive });
        });

        // Configure Spec entity
        modelBuilder.Entity<Spec>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Language).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LineCount).IsRequired();
            entity.Property(e => e.SizeInBytes).IsRequired();
            entity.Property(e => e.ParsedMetadata).HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Relationships
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Specs)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Language);
            entity.HasIndex(e => e.FileType);
        });

        // Configure Passport entity
        modelBuilder.Entity<Passport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectId).IsRequired();
            entity.Property(e => e.Version).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DocumentationContent).IsRequired();
            entity.Property(e => e.Format).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.GeneratedAt).IsRequired();
            entity.Property(e => e.GeneratedBy).IsRequired();
            entity.Property(e => e.SizeInBytes).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            // Relationships
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Passports)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.GeneratedBy);
            entity.HasIndex(e => new { e.ProjectId, e.GeneratedAt });
        });

        // Configure enum conversion
        modelBuilder.Entity<Passport>()
            .Property(e => e.Status)
            .HasConversion<string>();
    }
}

