using MediatR;

namespace AutoDocOps.Application.Passports.Queries.GetPassport;

public record GetPassportQuery(Guid Id) : IRequest<GetPassportResponse>;
