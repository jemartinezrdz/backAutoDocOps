using MediatR;

namespace AutoDocOps.Application.Projects.Queries.GetProject;

public record GetProjectQuery(Guid Id) : IRequest<GetProjectResponse>;
