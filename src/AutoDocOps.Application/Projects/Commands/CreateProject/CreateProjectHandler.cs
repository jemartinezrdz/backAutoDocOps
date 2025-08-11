using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using MediatR;

namespace AutoDocOps.Application.Projects.Commands.CreateProject;

public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, CreateProjectResponse>
{
    private readonly IProjectRepository _projectRepository;

    public CreateProjectHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<CreateProjectResponse> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
    ArgumentNullException.ThrowIfNull(request);
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            RepositoryUrl = request.RepositoryUrl,
            Branch = request.Branch,
            OrganizationId = request.OrganizationId,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

    var createdProject = await _projectRepository.CreateAsync(project, cancellationToken).ConfigureAwait(false);

    return new CreateProjectResponse(
            createdProject.Id,
            createdProject.Name,
            createdProject.Description,
            createdProject.RepositoryUrl,
            createdProject.Branch,
            createdProject.OrganizationId,
            createdProject.CreatedBy,
            createdProject.CreatedAt
        );
    }
}

