using MediatR;

namespace AutoDocOps.Application.Passports.Queries.GetGenerationStatus;

public record GetGenerationStatusQuery(Guid PassportId) : IRequest<GetGenerationStatusResponse>;
