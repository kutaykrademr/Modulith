using Auth.Domain.Abstractions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace Auth.Infrastructure.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public SmtpEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var emailConfig = _configuration.GetSection("Email");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            emailConfig["SenderName"] ?? "Modulith",
            emailConfig["SenderEmail"] ?? "noreply@modulith.dev"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        message.Body = new TextPart("html")
        {
            Text = htmlBody
        };

        using var client = new SmtpClient();

        var host = emailConfig["SmtpHost"] ?? "localhost";
        var port = int.Parse(emailConfig["SmtpPort"] ?? "1025");
        var enableSsl = bool.Parse(emailConfig["EnableSsl"] ?? "false");

        await client.ConnectAsync(host, port, enableSsl, cancellationToken);

        var username = emailConfig["Username"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            await client.AuthenticateAsync(username, emailConfig["Password"], cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
