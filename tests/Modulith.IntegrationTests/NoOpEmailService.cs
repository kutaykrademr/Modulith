using Auth.Domain.Abstractions;

namespace Modulith.IntegrationTests;

/// <summary>Testlerde gerçek SMTP yerine kullanılan, hiçbir şey yapmayan email servisi.</summary>
public sealed class NoOpEmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
