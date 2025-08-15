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
    ArgumentNullException.ThrowIfNull(request);
        // Verify project exists
    var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
        if (project == null)
        {
            throw new ArgumentException($"Project with ID {request.ProjectId} not found.");
        }

        // Create passport entity
        var passport = new Passport
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Version = string.IsNullOrWhiteSpace(request.Version) ? "1.0.0" : request.Version,
            Format = string.IsNullOrWhiteSpace(request.Format) ? "markdown" : request.Format,
            Status = PassportStatus.Generating,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = request.GeneratedBy,
            DocumentationContent = string.Empty // Will be populated by background service
        };

    var createdPassport = await _passportRepository.CreateAsync(passport, cancellationToken).ConfigureAwait(false);

        // Background service will pick up the passport with "Generating" status and process it

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

