using System.Security.Cryptography;
using Auth.Domain.Services;

namespace Auth.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    // Email Verification — token DB'de SHA-256 özeti olarak saklanır (düz metin değil).
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationTokenHash { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAtUtc { get; private set; }

    // Password Reset — token DB'de SHA-256 özeti olarak saklanır (düz metin değil).
    public string? PasswordResetTokenHash { get; private set; }
    public DateTime? PasswordResetTokenExpiresAtUtc { get; private set; }

    private User() { } // EF Core için parameterless constructor

    public static User Create(string email, string fullName, string passwordHash)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            FullName = fullName,
            PasswordHash = passwordHash,
            CreatedAtUtc = DateTime.UtcNow,
            IsEmailVerified = false
        };

        return user;
    }

    /// <summary>
    /// Yeni bir email doğrulama token'ı üretir (24 saat geçerli).
    /// Ham token'ı döner (e-postaya konacak); DB'ye yalnızca özeti yazılır.
    /// </summary>
    public string GenerateEmailVerificationToken()
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        EmailVerificationTokenHash = TokenHasher.Hash(rawToken);
        EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddHours(24);
        return rawToken;
    }

    /// <summary>
    /// Email doğrulamasını tamamlar. Token geçersizse veya süresi dolmuşsa false döner.
    /// </summary>
    public bool VerifyEmail(string token)
    {
        if (IsEmailVerified)
            return true;

        if (EmailVerificationTokenHash is null || !TokenHasher.Verify(token, EmailVerificationTokenHash))
            return false;

        if (EmailVerificationTokenExpiresAtUtc.HasValue && DateTime.UtcNow > EmailVerificationTokenExpiresAtUtc.Value)
            return false;

        IsEmailVerified = true;
        EmailVerificationTokenHash = null;
        EmailVerificationTokenExpiresAtUtc = null;
        return true;
    }

    /// <summary>
    /// Yeni bir şifre sıfırlama token'ı üretir (1 saat geçerli).
    /// Ham token'ı döner (e-postaya konacak); DB'ye yalnızca özeti yazılır.
    /// </summary>
    public string GeneratePasswordResetToken()
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        PasswordResetTokenHash = TokenHasher.Hash(rawToken);
        PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1);
        return rawToken;
    }

    /// <summary>
    /// Şifreyi sıfırlar. Token geçersizse veya süresi dolmuşsa false döner.
    /// </summary>
    public bool ResetPassword(string token, string newPasswordHash)
    {
        if (PasswordResetTokenHash is null || !TokenHasher.Verify(token, PasswordResetTokenHash))
            return false;

        if (PasswordResetTokenExpiresAtUtc.HasValue && DateTime.UtcNow > PasswordResetTokenExpiresAtUtc.Value)
            return false;

        PasswordHash = newPasswordHash;
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAtUtc = null;
        return true;
    }
}
