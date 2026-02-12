namespace Auth.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    private User() { } // EF Core için parameterless constructor

    public static User Create(string email, string fullName, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            FullName = fullName,
            PasswordHash = passwordHash,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
