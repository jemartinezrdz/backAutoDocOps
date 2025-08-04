using MediatR;

namespace AutoDocOps.Application.Passports.Commands.GeneratePassport;

public record GeneratePassportCommand(
    Guid ProjectId,
    string Version,
    string Format,
    Guid GeneratedBy
) : IRequest<GeneratePassportResponse>;

