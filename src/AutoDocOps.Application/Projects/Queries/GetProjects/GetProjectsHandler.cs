using AutoDocOps.Domain.Interfaces;
using MediatR;

namespace AutoDocOps.Application.Projects.Queries.GetProjects;

public class GetProjectsHandler : IRequestHandler<GetProjectsQuery, GetProjectsResponse>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectsHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<GetProjectsResponse> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var projects = await _projectRepository.GetByOrganizationIdAsync(
            request.OrganizationId, 
            request.Page, 
            request.PageSize, 
            cancellationToken);

        var totalCount = await _projectRepository.GetCountByOrganizationIdAsync(
            request.OrganizationId, 
            cancellationToken);

        var projectDtos = projects.Select(p => new ProjectDto(
            p.Id,
            p.Name,
            p.Description,
            p.RepositoryUrl,
            p.Branch,
            p.OrganizationId,
            p.CreatedBy,
            p.CreatedAt,
            p.UpdatedAt,
            p.IsActive
        ));

        return new GetProjectsResponse(
            projectDtos,
            totalCount,
            request.Page,
            request.PageSize
        );
    }
}

