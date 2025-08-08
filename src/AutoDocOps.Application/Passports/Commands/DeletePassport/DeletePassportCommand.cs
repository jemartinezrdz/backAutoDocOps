using MediatR;

namespace AutoDocOps.Application.Passports.Commands.DeletePassport;

public record DeletePassportCommand(Guid Id) : IRequest<DeletePassportResponse>;
