using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Application.Projects.Queries.GetProjects;
using MediatR;

namespace AutoDocOps.Application.Projects.Queries.GetProject;

public class GetProjectHandler : IRequestHandler<GetProjectQuery, GetProjectResponse>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<GetProjectResponse> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (project == null)
        {
            throw new ArgumentException($"Project with ID {request.Id} not found.");
        }

        var projectDto = new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            project.RepositoryUrl,
            project.Branch,
            project.OrganizationId,
            project.CreatedBy,
            project.CreatedAt,
            project.UpdatedAt,
            project.IsActive
        );

        return new GetProjectResponse(projectDto);
    }
}
