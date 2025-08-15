using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Application.Passports.Queries.GetPassport;
using MediatR;

namespace AutoDocOps.Application.Passports.Queries.GetPassportsByProject;

public class GetPassportsByProjectHandler : IRequestHandler<GetPassportsByProjectQuery, GetPassportsByProjectResponse>
{
    private readonly IPassportRepository _passportRepository;

    public GetPassportsByProjectHandler(IPassportRepository passportRepository)
    {
        _passportRepository = passportRepository;
    }

    public async Task<GetPassportsByProjectResponse> Handle(GetPassportsByProjectQuery request, CancellationToken cancellationToken)
    {
    ArgumentNullException.ThrowIfNull(request);
    var passports = await _passportRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken).ConfigureAwait(false);
        
        // Apply pagination
        var totalCount = passports.Count();
        var paginatedPassports = passports
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize);

        var passportDtos = paginatedPassports.Select(p => new PassportDto(
            p.Id,
            p.ProjectId,
            p.Version,
            p.DocumentationContent,
            p.Format,
            p.Metadata,
            p.Status,
            p.GeneratedAt,
            p.CompletedAt,
            p.GeneratedBy,
            p.SizeInBytes,
            p.ErrorMessage
        ));

        return new GetPassportsByProjectResponse(
            passportDtos,
            totalCount,
            request.Page,
            request.PageSize
        );
    }
}
