using Auth.Domain.Abstractions;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Auth.Application.ResendVerification;

public sealed class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ResendVerificationCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<bool> Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
        if (user is null)
            return false;

        if (user.IsEmailVerified)
            throw new InvalidOperationException("Bu email adresi zaten doğrulanmış.");

        // Yeni token üret
        user.GenerateEmailVerificationToken();
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Doğrulama emaili gönder
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5116";
        var verificationLink = $"{baseUrl}/api/auth/verify-email?email={Uri.EscapeDataString(user.Email)}&token={user.EmailVerificationToken}";

        var htmlBody = $"""
            <h2>Email Doğrulama</h2>
            <p>Merhaba {user.FullName},</p>
            <p>Email adresinizi doğrulamak için aşağıdaki bağlantıya tıklayın:</p>
            <p><a href="{verificationLink}">Email Adresimi Doğrula</a></p>
            <p>Bu bağlantı 24 saat geçerlidir.</p>
            <p>Bu işlemi siz yapmadıysanız, bu emaili görmezden gelebilirsiniz.</p>
            """;

        await _emailService.SendEmailAsync(user.Email, "Email Adresinizi Doğrulayın", htmlBody, cancellationToken);

        return true;
    }
}
