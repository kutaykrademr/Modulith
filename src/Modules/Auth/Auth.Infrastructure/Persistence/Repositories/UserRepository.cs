using Auth.Domain.Abstractions;
using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using Shared.Infrastructure.Persistence;

namespace Auth.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ModulithDbContext _context;

    public UserRepository(ModulithDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Set<User>().AddAsync(user, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<User>()
            .AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
