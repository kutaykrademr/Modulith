using Auth.Domain.Abstractions;

namespace Auth.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    // Kullanıcı bulunamadığında, gerçek doğrulamayla aynı maliyette bir karşılaştırma
    // yapabilmek için process başına bir kez üretilen geçerli bir bcrypt hash'i.
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword("dummy-password-for-timing-equalization", WorkFactor);

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public void VerifyDummy(string password)
    {
        // Sonucu önemli değil; amaç yalnızca CPU maliyetini eşitlemek.
        BCrypt.Net.BCrypt.Verify(password, DummyHash);
    }
}
