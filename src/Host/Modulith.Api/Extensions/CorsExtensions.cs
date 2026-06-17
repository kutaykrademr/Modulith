namespace Modulith.Api.Extensions;

/// <summary>
/// Yapılandırmadan (Cors:AllowedOrigins) okunan origin listesine göre CORS politikası kurar.
/// Liste boşsa politika kayıt edilmez; tarayıcı yalnızca aynı-origin isteklerine izin verir.
/// </summary>
public static class CorsExtensions
{
    public const string PolicyName = "ModulithCors";

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (origins.Length == 0)
                    return;

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
