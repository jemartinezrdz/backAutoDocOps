using AutoDocOps.Application.Passports.Queries.GetPassport;

namespace AutoDocOps.Application.Passports.Queries.GetPassportsByProject;

public record GetPassportsByProjectResponse(
    IEnumerable<PassportDto> Passports,
    int TotalCount,
    int Page,
    int PageSize
);
