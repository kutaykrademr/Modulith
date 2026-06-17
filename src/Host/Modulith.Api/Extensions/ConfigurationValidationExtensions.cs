namespace Modulith.Api.Extensions;

/// <summary>
/// Uygulama açılışında kritik yapılandırmanın (secret'lar, connection string) var ve
/// geçerli olduğunu doğrular. Eksik secret ile sessizce başlamak yerine erken ve net hata verir.
/// </summary>
public static class ConfigurationValidationExtensions
{
    public static void ValidateCriticalConfiguration(this IConfiguration configuration)
    {
        var errors = new List<string>();

        var jwtSecret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            errors.Add("Jwt:Secret tanımlı değil. Dev için 'dotnet user-secrets set \"Jwt:Secret\" <değer>', prod için Jwt__Secret environment variable kullanın.");
        }
        else if (jwtSecret.Length < 32)
        {
            errors.Add($"Jwt:Secret en az 32 karakter olmalı (şu an {jwtSecret.Length}). HMAC-SHA256 için yeterli entropi gerekir.");
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]))
            errors.Add("Jwt:Issuer tanımlı değil.");

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
            errors.Add("Jwt:Audience tanımlı değil.");

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("ModulithDb")))
            errors.Add("ConnectionStrings:ModulithDb tanımlı değil.");

        if (errors.Count != 0)
        {
            throw new InvalidOperationException(
                "Geçersiz uygulama yapılandırması:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(e => " - " + e)));
        }
    }
}
