using MediatR;

namespace AutoDocOps.Application.Projects.Queries.GetProjects;

public record GetProjectsQuery(
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 10
) : IRequest<GetProjectsResponse>;

