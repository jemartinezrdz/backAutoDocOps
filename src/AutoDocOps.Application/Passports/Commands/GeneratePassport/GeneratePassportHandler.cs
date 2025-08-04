using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using MediatR;

namespace AutoDocOps.Application.Passports.Commands.GeneratePassport;

public class GeneratePassportHandler : IRequestHandler<GeneratePassportCommand, GeneratePassportResponse>
{
    private readonly IPassportRepository _passportRepository;
    private readonly IProjectRepository _projectRepository;

    public GeneratePassportHandler(
        IPassportRepository passportRepository,
        IProjectRepository projectRepository)
    {
        _passportRepository = passportRepository;
        _projectRepository = projectRepository;
    }

    public async Task<GeneratePassportResponse> Handle(GeneratePassportCommand request, CancellationToken cancellationToken)
    {
        // Verify project exists
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project == null)
        {
            throw new ArgumentException($"Project with ID {request.ProjectId} not found.");
        }

        // Create passport entity
        var passport = new Passport
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Version = request.Version,
            Format = request.Format,
            Status = PassportStatus.Generating,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = request.GeneratedBy,
            DocumentationContent = string.Empty // Will be populated by background service
        };

        var createdPassport = await _passportRepository.CreateAsync(passport, cancellationToken);

        // TODO: Trigger background job to process the documentation generation
        // This would involve:
        // 1. Clone repository
        // 2. Analyze code with IL Scanner
        // 3. Generate documentation
        // 4. Update passport status and content

        return new GeneratePassportResponse(
            createdPassport.Id,
            createdPassport.ProjectId,
            createdPassport.Version,
            createdPassport.Format,
            createdPassport.Status,
            createdPassport.GeneratedAt,
            createdPassport.GeneratedBy
        );
    }
}

