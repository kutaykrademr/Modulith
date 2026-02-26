using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Modulith.Api.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitConfig = configuration.GetSection("RateLimiting");

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 1. Genel API Koruması (Örn: /api/users, /api/products)
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.PermitLimit = rateLimitConfig.GetValue<int>("Fixed:PermitLimit");
                opt.Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("Fixed:WindowSeconds"));
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // 2. Auth Module - Login Endpoint (Brute-force koruması)
            options.AddFixedWindowLimiter("login", opt =>
            {
                opt.PermitLimit = rateLimitConfig.GetValue<int>("Login:PermitLimit");
                opt.Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("Login:WindowSeconds"));
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // 3. Auth Module - Register/Forgot Password Endpoints (Spam koruması)
            options.AddFixedWindowLimiter("register", opt =>
            {
                opt.PermitLimit = rateLimitConfig.GetValue<int>("Register:PermitLimit");
                opt.Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("Register:WindowSeconds"));
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
            
            // İleride buraya başka modüller eklenebilir
            // Örneğin Users modülü için aşırı istekleri kısıtlamak isterseniz:
            /*
            options.AddFixedWindowLimiter("users", opt =>
            {
                opt.PermitLimit = ...;
                opt.Window = ...;
            });
            */
        });

        return services;
    }
}
