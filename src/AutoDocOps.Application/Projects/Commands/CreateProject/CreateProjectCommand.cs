using MediatR;

namespace AutoDocOps.Application.Projects.Commands.CreateProject;

public record CreateProjectCommand(
    string Name,
    string? Description,
    string RepositoryUrl,
    string Branch,
    Guid OrganizationId,
    Guid CreatedBy
) : IRequest<CreateProjectResponse>;

