using AutoDocOps.Domain.Entities;

namespace AutoDocOps.Application.Passports.Queries.GetGenerationStatus;

public record GetGenerationStatusResponse(
    Guid PassportId,
    PassportStatus Status,
    int ProgressPercentage,
    string? CurrentStep,
    string? ErrorMessage,
    DateTime? EstimatedCompletion
);
