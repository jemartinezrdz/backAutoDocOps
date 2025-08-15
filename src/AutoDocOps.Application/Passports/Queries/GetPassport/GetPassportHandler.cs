using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Domain.Entities;
using MediatR;

namespace AutoDocOps.Application.Passports.Queries.GetPassport;

public class GetPassportHandler : IRequestHandler<GetPassportQuery, GetPassportResponse>
{
    private readonly IPassportRepository _passportRepository;

    public GetPassportHandler(IPassportRepository passportRepository)
    {
        _passportRepository = passportRepository;
    }

    public async Task<GetPassportResponse> Handle(GetPassportQuery request, CancellationToken cancellationToken)
    {
    ArgumentNullException.ThrowIfNull(request);
    var passport = await _passportRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        
        if (passport == null)
        {
            throw new ArgumentException($"Passport with ID {request.Id} not found.");
        }

        return new GetPassportResponse(
            passport.Id,
            passport.ProjectId,
            passport.Version,
            passport.DocumentationContent,
            passport.Format,
            passport.Metadata,
            passport.Status,
            passport.GeneratedAt,
            passport.CompletedAt,
            passport.GeneratedBy,
            passport.SizeInBytes,
            passport.ErrorMessage
        );
    }
}
