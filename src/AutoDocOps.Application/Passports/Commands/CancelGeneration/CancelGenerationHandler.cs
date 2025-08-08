using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Domain.Entities;
using MediatR;

namespace AutoDocOps.Application.Passports.Commands.CancelGeneration;

public class CancelGenerationHandler : IRequestHandler<CancelGenerationCommand, CancelGenerationResponse>
{
    private readonly IPassportRepository _passportRepository;

    public CancelGenerationHandler(IPassportRepository passportRepository)
    {
        _passportRepository = passportRepository;
    }

    public async Task<CancelGenerationResponse> Handle(CancelGenerationCommand request, CancellationToken cancellationToken)
    {
        var passport = await _passportRepository.GetByIdAsync(request.PassportId, cancellationToken);
        
        if (passport == null)
        {
            return new CancelGenerationResponse(false, "Passport not found");
        }

        if (passport.Status != PassportStatus.Generating)
        {
            return new CancelGenerationResponse(false, "Cannot cancel: Documentation generation is not in progress");
        }

        // Update passport status to cancelled
        passport.Status = PassportStatus.Cancelled;
        passport.CompletedAt = DateTime.UtcNow;
        passport.ErrorMessage = $"Generation cancelled by user {request.CancelledBy}";

        await _passportRepository.UpdateAsync(passport, cancellationToken);

        // Note: The background service will detect the cancelled status and stop processing

        return new CancelGenerationResponse(true, "Documentation generation cancelled successfully");
    }
}
