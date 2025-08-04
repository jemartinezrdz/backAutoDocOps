namespace AutoDocOps.Application.Projects.Queries.GetProjects;

public record GetProjectsResponse(
    IEnumerable<ProjectDto> Projects,
    int TotalCount,
    int Page,
    int PageSize
);

public record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    string RepositoryUrl,
    string Branch,
    Guid OrganizationId,
    Guid CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsActive
);

