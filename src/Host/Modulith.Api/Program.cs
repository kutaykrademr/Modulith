using System.Threading.RateLimiting;
using Auth.Infrastructure;
using Auth.Infrastructure.Persistence;
using Auth.Presentation;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// Rate Limiting — Fixed Window, IP bazlı
var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Genel API koruma
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = rateLimitConfig.GetValue<int>("Fixed:PermitLimit");
        opt.Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("Fixed:WindowSeconds"));
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    // Login endpoint — brute-force koruması
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = rateLimitConfig.GetValue<int>("Login:PermitLimit");
        opt.Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("Login:WindowSeconds"));
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    // Register endpoint — spam koruması
    options.AddFixedWindowLimiter("register", opt =>
    {
        opt.PermitLimit = rateLimitConfig.GetValue<int>("Register:PermitLimit");
        opt.Window = TimeSpan.FromSeconds(rateLimitConfig.GetValue<int>("Register:WindowSeconds"));
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// Auth Module — tüm DI kayıtları burada
builder.Services.AddAuthModule(builder.Configuration);

var app = builder.Build();

// Development ortamında DB'yi otomatik oluştur (migration yerine EnsureCreated)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // → http://localhost:5116/scalar/v1
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Modulith API");
    });

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();

// Rate Limiter middleware — Authentication'dan önce
app.UseRateLimiter();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Auth endpoints — /api/auth/*
app.MapAuthEndpoints();

app.Run();
