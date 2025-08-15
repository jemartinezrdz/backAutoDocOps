using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoDocOps.Infrastructure.Repositories;

public class SpecRepository : ISpecRepository
{
    private readonly AutoDocOpsDbContext _context;

    public SpecRepository(AutoDocOpsDbContext context)
    {
        _context = context;
    }

    public async Task<Spec?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Specs
            .Include(s => s.Project)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Spec>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Specs
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.FilePath)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Spec> CreateAsync(Spec spec, CancellationToken cancellationToken = default)
    {
        _context.Specs.Add(spec);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return spec;
    }

    public async Task<Spec> UpdateAsync(Spec spec, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spec);
        spec.UpdatedAt = DateTime.UtcNow;
        _context.Specs.Update(spec);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return spec;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var spec = await _context.Specs.FindAsync(new object[] { id }, cancellationToken).ConfigureAwait(false);
        if (spec != null)
        {
            _context.Specs.Remove(spec);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var specs = await _context.Specs
            .Where(s => s.ProjectId == projectId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (specs.Count > 0)
        {
            _context.Specs.RemoveRange(specs);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        return false;
    }

    public async Task<IEnumerable<Spec>> CreateBatchAsync(IEnumerable<Spec> specs, CancellationToken cancellationToken = default)
    {
        var specList = specs.ToList();
        _context.Specs.AddRange(specList);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return specList;
    }
}

