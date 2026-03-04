using Auth.Infrastructure;
using User.Infrastructure;
using Shared.Infrastructure.Persistence;
using Shared.Contracts.Abstractions;
using Microsoft.EntityFrameworkCore;
using Auth.Presentation;
using User.Presentation;
using Scalar.AspNetCore;
using Modulith.Api.Extensions;
using Modulith.Api.Extensions.Transformers;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerSecurityOperationTransformer>();
});

// Rate Limiting (Ayrı dosyadan merkezi yönetim)
builder.Services.AddCustomRateLimiting(builder.Configuration);

// EF Core - PostgreSQL (Shared DbContext)
builder.Services.AddDbContext<ModulithDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("ModulithDb"),
        npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory")));

// Unit of Work — modüller arası transaction yönetimi
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Auth Module — tüm DI kayıtları burada
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddUserModule(builder.Configuration);

var app = builder.Build();

// Development ortamında DB'yi otomatik oluştur.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // → http://localhost:5116/scalar/v1
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Modulith API");
    });

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ModulithDbContext>();

    await dbContext.Database.EnsureDeletedAsync();
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
app.MapUserEndpoints();

app.Run();
