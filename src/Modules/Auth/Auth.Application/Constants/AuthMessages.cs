namespace Auth.Application.Constants;

public static class AuthMessages
{
    public const string InvalidToken = "Geçersiz token.";
    public const string LogoutSuccessful = "Başarıyla çıkış yapıldı.";
    
    public const string EmailVerified = "Email adresiniz başarıyla doğrulandı.";
    public const string InvalidOrExpiredVerificationLink = "Geçersiz veya süresi dolmuş doğrulama bağlantısı.";
    public const string VerificationEmailResent = "Doğrulama emaili tekrar gönderildi.";
    
    public const string UserNotFound = "Kullanıcı bulunamadı.";
    
    public const string PasswordResetLinkSent = "Şifre sıfırlama bağlantısı email adresinize gönderildi.";
    public const string PasswordUpdated = "Şifreniz başarıyla güncellendi.";
    public const string InvalidOrExpiredResetLink = "Geçersiz veya süresi dolmuş sıfırlama bağlantısı.";
    
    public const string InvalidEmailOrPassword = "Geçersiz email veya şifre.";
    public const string EmailNotVerified = "Email adresiniz doğrulanmamış. Lütfen email kutunuzu kontrol edin.";
    public const string EmailAlreadyVerified = "Bu email adresi zaten doğrulanmış.";
    public const string InvalidOrExpiredRefreshToken = "Geçersiz veya süresi dolmuş refresh token.";
    
    public static string UserAlreadyExists(string email) => $"'{email}' adresi ile kayıtlı bir kullanıcı zaten mevcut.";
}
