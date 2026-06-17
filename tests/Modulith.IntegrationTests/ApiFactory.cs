using Auth.Domain.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace Modulith.IntegrationTests;

/// <summary>
/// Testler için izole bir PostgreSQL container'ı ayağa kaldırır ve uygulamayı bu DB'ye
/// bağlayacak şekilde yapılandırır. E-posta gönderimi sahte (no-op) servisle değiştirilir.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ModulithDb"] = _db.GetConnectionString(),
                ["Jwt:Secret"] = "integration-test-secret-key-at-least-32-bytes-long!!",
                ["Database:ResetOnStartup"] = "false",
                // Testlerde rate limit'i devre dışı bırakmamak ama engel olmaması için yüksek tut.
                ["RateLimiting:Fixed:PermitLimit"] = "100000",
                ["RateLimiting:Login:PermitLimit"] = "100000",
                ["RateLimiting:Register:PermitLimit"] = "100000"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Gerçek SMTP yerine no-op email servisi.
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, NoOpEmailService>();
        });
    }

    public async Task InitializeAsync() => await _db.StartAsync();

    public new async Task DisposeAsync() => await _db.DisposeAsync();
}
