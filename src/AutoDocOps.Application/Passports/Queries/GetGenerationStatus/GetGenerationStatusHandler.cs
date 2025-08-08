using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Domain.Entities;
using MediatR;

namespace AutoDocOps.Application.Passports.Queries.GetGenerationStatus;

public class GetGenerationStatusHandler : IRequestHandler<GetGenerationStatusQuery, GetGenerationStatusResponse>
{
    private readonly IPassportRepository _passportRepository;

    public GetGenerationStatusHandler(IPassportRepository passportRepository)
    {
        _passportRepository = passportRepository;
    }

    public async Task<GetGenerationStatusResponse> Handle(GetGenerationStatusQuery request, CancellationToken cancellationToken)
    {
        var passport = await _passportRepository.GetByIdAsync(request.PassportId, cancellationToken);
        
        if (passport == null)
        {
            throw new ArgumentException($"Passport with ID {request.PassportId} not found.");
        }

        // Calculate progress based on status
        var progressPercentage = passport.Status switch
        {
            PassportStatus.Generating => 50, // In progress
            PassportStatus.Completed => 100,
            PassportStatus.Failed => 0,
            PassportStatus.Cancelled => 0,
            _ => 0
        };

        var currentStep = passport.Status switch
        {
            PassportStatus.Generating => "Analyzing code and generating documentation...",
            PassportStatus.Completed => "Documentation generation completed",
            PassportStatus.Failed => "Documentation generation failed",
            PassportStatus.Cancelled => "Documentation generation cancelled",
            _ => "Unknown status"
        };

        // Estimate completion time (simple logic)
        DateTime? estimatedCompletion = passport.Status == PassportStatus.Generating 
            ? passport.GeneratedAt.AddMinutes(10) // Estimate 10 minutes total
            : passport.CompletedAt;

        return new GetGenerationStatusResponse(
            passport.Id,
            passport.Status,
            progressPercentage,
            currentStep,
            passport.ErrorMessage,
            estimatedCompletion
        );
    }
}
