using AutoDocOps.Domain.Entities;

namespace AutoDocOps.Application.Passports.Commands.GeneratePassport;

public record GeneratePassportResponse(
    Guid Id,
    Guid ProjectId,
    string Version,
    string Format,
    PassportStatus Status,
    DateTime GeneratedAt,
    Guid GeneratedBy
);

