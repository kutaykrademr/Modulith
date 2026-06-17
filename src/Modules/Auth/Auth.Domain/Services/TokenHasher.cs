using System.Security.Cryptography;
using System.Text;

namespace Auth.Domain.Services;

/// <summary>
/// Bearer niteliğindeki opak token'ları (refresh token, email doğrulama, şifre sıfırlama)
/// veritabanında düz metin yerine SHA-256 özeti olarak saklamak için kullanılır.
/// Token'ın kendisi yalnızca kullanıcıya gönderilir; DB sızsa bile özetten token üretilemez.
/// </summary>
public static class TokenHasher
{
    /// <summary>Ham token'ın SHA-256 özetini (büyük harf hex) döner.</summary>
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Sabit zamanlı karşılaştırma — verilen ham token, saklanan özetle eşleşiyor mu?
    /// </summary>
    public static bool Verify(string token, string storedHash)
    {
        var computed = Hash(token);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(storedHash));
    }
}
