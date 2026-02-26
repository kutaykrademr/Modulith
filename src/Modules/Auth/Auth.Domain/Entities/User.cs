using System.Security.Cryptography;

namespace Auth.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    // Email Verification
    public bool IsEmailVerified { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiresAtUtc { get; private set; }

    // Password Reset
    public string? PasswordResetToken { get; private set; }
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

        user.GenerateEmailVerificationToken();

        return user;
    }

    /// <summary>
    /// Yeni bir email doğrulama token'ı üretir (24 saat geçerli).
    /// </summary>
    public void GenerateEmailVerificationToken()
    {
        EmailVerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        EmailVerificationTokenExpiresAtUtc = DateTime.UtcNow.AddHours(24);
    }

    /// <summary>
    /// Email doğrulamasını tamamlar. Token geçersizse veya süresi dolmuşsa false döner.
    /// </summary>
    public bool VerifyEmail(string token)
    {
        if (IsEmailVerified)
            return true;

        if (EmailVerificationToken is null ||
            !string.Equals(EmailVerificationToken, token, StringComparison.OrdinalIgnoreCase))
            return false;

        if (EmailVerificationTokenExpiresAtUtc.HasValue && DateTime.UtcNow > EmailVerificationTokenExpiresAtUtc.Value)
            return false;

        IsEmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiresAtUtc = null;
        return true;
    }

    /// <summary>
    /// Yeni bir şifre sıfırlama token'ı üretir (1 saat geçerli).
    /// </summary>
    public void GeneratePasswordResetToken()
    {
        PasswordResetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1);
    }

    /// <summary>
    /// Şifreyi sıfırlar. Token geçersizse veya süresi dolmuşsa false döner.
    /// </summary>
    public bool ResetPassword(string token, string newPasswordHash)
    {
        if (PasswordResetToken is null ||
            !string.Equals(PasswordResetToken, token, StringComparison.OrdinalIgnoreCase))
            return false;

        if (PasswordResetTokenExpiresAtUtc.HasValue && DateTime.UtcNow > PasswordResetTokenExpiresAtUtc.Value)
            return false;

        PasswordHash = newPasswordHash;
        PasswordResetToken = null;
        PasswordResetTokenExpiresAtUtc = null;
        return true;
    }
}
