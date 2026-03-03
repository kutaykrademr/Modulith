using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using User.Domain.Abstractions;
using User.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace User.Infrastructure.Persistence.Repositories;

public sealed class ProfileRepository : IProfileRepository
{
    private readonly ModulithDbContext _context;

    public ProfileRepository(ModulithDbContext context)
    {
        _context = context;
    }

    public async Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Profile>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Profile>()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public void Add(Profile profile)
    {
        _context.Set<Profile>().Add(profile);
    }

    public void Update(Profile profile)
    {
        _context.Set<Profile>().Update(profile);
    }
}
