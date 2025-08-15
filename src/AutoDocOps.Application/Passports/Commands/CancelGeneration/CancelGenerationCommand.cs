using MediatR;

namespace AutoDocOps.Application.Passports.Commands.CancelGeneration;

public record CancelGenerationCommand(Guid PassportId, Guid CancelledBy) : IRequest<CancelGenerationResponse>;
