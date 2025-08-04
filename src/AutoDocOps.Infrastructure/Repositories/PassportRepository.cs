using AutoDocOps.Domain.Entities;
using AutoDocOps.Domain.Interfaces;
using AutoDocOps.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoDocOps.Infrastructure.Repositories;

public class PassportRepository : IPassportRepository
{
    private readonly AutoDocOpsDbContext _context;

    public PassportRepository(AutoDocOpsDbContext context)
    {
        _context = context;
    }

    public async Task<Passport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Passports
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Passport>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Passports
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Passport?> GetLatestByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.Passports
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.GeneratedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Passport>> GetByStatusAsync(PassportStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Passports
            .Where(p => p.Status == status)
            .Include(p => p.Project)
            .OrderBy(p => p.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Passport> CreateAsync(Passport passport, CancellationToken cancellationToken = default)
    {
        _context.Passports.Add(passport);
        await _context.SaveChangesAsync(cancellationToken);
        return passport;
    }

    public async Task<Passport> UpdateAsync(Passport passport, CancellationToken cancellationToken = default)
    {
        _context.Passports.Update(passport);
        await _context.SaveChangesAsync(cancellationToken);
        return passport;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var passport = await _context.Passports.FindAsync(new object[] { id }, cancellationToken);
        if (passport != null)
        {
            _context.Passports.Remove(passport);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }
}

