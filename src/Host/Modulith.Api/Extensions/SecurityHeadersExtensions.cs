namespace Modulith.Api.Extensions;

/// <summary>
/// Tüm yanıtlara temel güvenlik header'ları ekler. Bir API için makul varsayılanlar;
/// HTML sunan bir uygulama için CSP gibi ek başlıklar gerekebilir.
/// </summary>
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            // API JSON döndürdüğü için katı bir CSP yeterli.
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            await next();
        });
    }
}
