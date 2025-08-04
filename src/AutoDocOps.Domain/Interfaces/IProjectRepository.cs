using AutoDocOps.Domain.Entities;

namespace AutoDocOps.Domain.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdWithSpecsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetByOrganizationIdAsync(Guid organizationId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Project> CreateAsync(Project project, CancellationToken cancellationToken = default);
    Task<Project> UpdateAsync(Project project, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetCountByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
}

