using AutoDocOps.Domain.Entities;

namespace AutoDocOps.Tests.Builders;

public class ProjectBuilder
{
    private readonly Project _project = new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Project",
        Description = "Test Description",
        RepositoryUrl = "https://github.com/test/repo",
        Branch = "main",
        OrganizationId = Guid.NewGuid(),
        CreatedBy = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true
    };

    public ProjectBuilder WithId(Guid id) { _project.Id = id; return this; }
    public ProjectBuilder WithName(string name) { _project.Name = name; return this; }
    public ProjectBuilder WithDescription(string description) { _project.Description = description; return this; }
    public ProjectBuilder WithRepository(string url, string branch = "main") { _project.RepositoryUrl = url; _project.Branch = branch; return this; }
    public ProjectBuilder WithOrganization(Guid orgId) { _project.OrganizationId = orgId; return this; }
    public ProjectBuilder WithCreatedBy(Guid userId) { _project.CreatedBy = userId; return this; }
    public ProjectBuilder Inactive() { _project.IsActive = false; return this; }

    public Project Build() => _project;
}
