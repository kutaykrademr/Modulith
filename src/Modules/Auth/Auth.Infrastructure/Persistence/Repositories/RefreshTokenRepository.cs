using Auth.Domain.Abstractions;
using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

using Shared.Infrastructure.Persistence;

namespace Auth.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ModulithDbContext _context;

    public RefreshTokenRepository(ModulithDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.Set<RefreshToken>().AddAsync(refreshToken, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.Set<RefreshToken>()
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke();
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
