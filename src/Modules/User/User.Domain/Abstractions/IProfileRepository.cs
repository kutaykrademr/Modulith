using User.Domain.Entities;

namespace User.Domain.Abstractions;

public interface IProfileRepository
{
    Task<Profile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Profile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(Profile profile);
    void Update(Profile profile);
}
