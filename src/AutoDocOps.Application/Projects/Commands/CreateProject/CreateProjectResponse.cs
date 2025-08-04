namespace AutoDocOps.Application.Projects.Commands.CreateProject;

public record CreateProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    string RepositoryUrl,
    string Branch,
    Guid OrganizationId,
    Guid CreatedBy,
    DateTime CreatedAt
);

