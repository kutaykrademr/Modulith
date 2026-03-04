using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Contracts.Abstractions;

namespace Shared.Infrastructure.Persistence;

/// <summary>
/// ModulithDbContext üzerinden Unit of Work implementasyonu.
/// Modüller arası işlemlerde tek bir DB transaction'ı yönetir.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ModulithDbContext _dbContext;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(ModulithDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction ??= await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("Transaction başlatılmadan commit yapılamaz.");

        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return;

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
