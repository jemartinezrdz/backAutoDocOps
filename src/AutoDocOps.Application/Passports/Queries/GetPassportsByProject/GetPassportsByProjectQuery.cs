using MediatR;

namespace AutoDocOps.Application.Passports.Queries.GetPassportsByProject;

public record GetPassportsByProjectQuery(
    Guid ProjectId,
    int Page = 1,
    int PageSize = 10
) : IRequest<GetPassportsByProjectResponse>;
