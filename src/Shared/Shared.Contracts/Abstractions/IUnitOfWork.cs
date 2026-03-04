namespace Shared.Contracts.Abstractions;

/// <summary>
/// Unit of Work abstraction — modüller arası transaction yönetimi sağlar.
/// BeginTransaction ile başlatılan transaction içinde tüm modüllerin değişiklikleri
/// atomik olarak commit veya rollback edilir.
/// </summary>
public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
