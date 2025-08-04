using AutoDocOps.Domain.Entities;

namespace AutoDocOps.Domain.Interfaces;

public interface ISpecRepository
{
    Task<Spec?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Spec>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Spec> CreateAsync(Spec spec, CancellationToken cancellationToken = default);
    Task<Spec> UpdateAsync(Spec spec, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> DeleteByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Spec>> CreateBatchAsync(IEnumerable<Spec> specs, CancellationToken cancellationToken = default);
}

