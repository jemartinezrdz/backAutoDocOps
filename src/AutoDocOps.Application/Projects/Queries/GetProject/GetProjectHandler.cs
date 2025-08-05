using AutoDocOps.Application.Common.Interfaces;
using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Application.Projects.Queries.GetProjects;
using MediatR;

namespace AutoDocOps.Application.Projects.Queries.GetProject;

public class GetProjectHandler : IRequestHandler<GetProjectQuery, GetProjectResponse>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICacheService _cacheService;

    public GetProjectHandler(IProjectRepository projectRepository, ICacheService cacheService)
    {
        _projectRepository = projectRepository;
        _cacheService = cacheService;
    }

    public async Task<GetProjectResponse> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"project:{request.Id}";
        
        // Try to get from cache first
        if (_cacheService.TryGet(cacheKey, out GetProjectResponse? cachedResponse) && cachedResponse != null)
        {
            return cachedResponse;
        }

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

        var response = new GetProjectResponse(projectDto);
        
        // Cache the response for 20 minutes
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(20), cancellationToken);

        return response;
    }
}
