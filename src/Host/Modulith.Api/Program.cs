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
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Kritik yapılandırmayı (secret'lar, connection string) erken doğrula — eksikse açılışta dur.
builder.Configuration.ValidateCriticalConfiguration();

// Structured logging (Serilog) — appsettings + konsol
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

// OpenAPI / Swagger
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerSecurityOperationTransformer>();
});

// Rate Limiting (Ayrı dosyadan merkezi yönetim)
builder.Services.AddCustomRateLimiting(builder.Configuration);

// CORS (Cors:AllowedOrigins yapılandırmasından)
builder.Services.AddCustomCors(builder.Configuration);

// EF Core - PostgreSQL (Shared DbContext)
var connectionString = builder.Configuration.GetConnectionString("ModulithDb");
builder.Services.AddDbContext<ModulithDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory")));

// Health checks — /health (DB dahil)
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!, name: "postgresql");

// Unit of Work — modüller arası transaction yönetimi
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Modüller — tüm DI kayıtları kendi içlerinde
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddUserModule(builder.Configuration);

var app = builder.Build();

// DB şemasını migration'larla uygula. Development'ta Database:ResetOnStartup=true ise sıfırla.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ModulithDbContext>();

    if (app.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>("Database:ResetOnStartup"))
    {
        await dbContext.Database.EnsureDeletedAsync();
    }

    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // → http://localhost:5116/scalar/v1
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Modulith API");
    });
}
else
{
    app.UseHsts();
}

app.UseSerilogRequestLogging();

app.UseGlobalExceptionHandler();

app.UseSecurityHeaders();

app.UseHttpsRedirection();

app.UseCors(CorsExtensions.PolicyName);

// Rate Limiter middleware — Authentication'dan önce
app.UseRateLimiter();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Health endpoint (anonim)
app.MapHealthChecks("/health");

// Module endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();

app.Run();

// Integration test projesinin WebApplicationFactory ile erişebilmesi için.
public partial class Program;
