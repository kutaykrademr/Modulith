using Auth.Domain.Abstractions;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Auth.Application.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);

        // Güvenlik: Kullanıcı bulunamasa bile true dön (email enumeration engelleme)
        if (user is null)
            return true;

        // Reset token üret ve kaydet
        user.GeneratePasswordResetToken();
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Şifre sıfırlama emaili gönder
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5116";
        var resetLink = $"{baseUrl}/api/v1/auth/password/reset?email={Uri.EscapeDataString(user.Email)}&token={user.PasswordResetToken}";

        var htmlBody = $"""
            <h2>Şifre Sıfırlama</h2>
            <p>Merhaba {user.FullName},</p>
            <p>Şifrenizi sıfırlamak için aşağıdaki bağlantıya tıklayın:</p>
            <p><a href="{resetLink}">Şifremi Sıfırla</a></p>
            <p>Bu bağlantı 1 saat geçerlidir.</p>
            <p>Eğer bu isteği siz yapmadıysanız, bu emaili görmezden gelebilirsiniz.</p>
            """;

        await _emailService.SendEmailAsync(user.Email, "Şifre Sıfırlama Talebi", htmlBody, cancellationToken);

        return true;
    }
}
