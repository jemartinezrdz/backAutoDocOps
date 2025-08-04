using AutoDocOps.Domain.Entities;

namespace AutoDocOps.Domain.Interfaces;

public interface IPassportRepository
{
    Task<Passport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Passport>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Passport?> GetLatestByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Passport> CreateAsync(Passport passport, CancellationToken cancellationToken = default);
    Task<Passport> UpdateAsync(Passport passport, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Passport>> GetByStatusAsync(PassportStatus status, CancellationToken cancellationToken = default);
}

