using AutoDocOps.Domain.Entities;

namespace AutoDocOps.Application.Passports.Queries.GetPassport;

public record GetPassportResponse(
    Guid Id,
    Guid ProjectId,
    string Version,
    string DocumentationContent,
    string Format,
    string? Metadata,
    PassportStatus Status,
    DateTime GeneratedAt,
    DateTime? CompletedAt,
    Guid GeneratedBy,
    long SizeInBytes,
    string? ErrorMessage
);

public record PassportDto(
    Guid Id,
    Guid ProjectId,
    string Version,
    string DocumentationContent,
    string Format,
    string? Metadata,
    PassportStatus Status,
    DateTime GeneratedAt,
    DateTime? CompletedAt,
    Guid GeneratedBy,
    long SizeInBytes,
    string? ErrorMessage
);
