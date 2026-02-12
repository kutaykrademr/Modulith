using Auth.Infrastructure;
using Auth.Infrastructure.Persistence;
using Auth.Presentation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// Auth Module — tüm DI kayıtları burada
builder.Services.AddAuthModule(builder.Configuration);

var app = builder.Build();

// Development ortamında DB'yi otomatik oluştur (migration yerine EnsureCreated)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Auth endpoints — /api/auth/*
app.MapAuthEndpoints();

app.Run();
