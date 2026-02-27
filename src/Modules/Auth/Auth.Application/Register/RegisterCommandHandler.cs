using Auth.Domain.Abstractions;
using Auth.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Auth.Application.Constants;

namespace Auth.Application.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Email zaten kayıtlı mı kontrol et
        var exists = await _userRepository.ExistsAsync(request.Email, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException(AuthMessages.UserAlreadyExists(request.Email));
        }

        // Password'ü hashle
        var passwordHash = _passwordHasher.Hash(request.Password);

        // User entity oluştur (token otomatik üretilir)
        var user = User.Create(request.Email, request.FullName, passwordHash);

        // Veritabanına kaydet
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Doğrulama emaili gönder
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5116";
        var verificationLink = $"{baseUrl}/api/v1/auth/email/verify?email={Uri.EscapeDataString(user.Email)}&token={user.EmailVerificationToken}";

        var htmlBody = $"""
            <h2>Hoş Geldiniz!</h2>
            <p>Merhaba {user.FullName},</p>
            <p>Kaydınız başarıyla oluşturuldu. Email adresinizi doğrulamak için aşağıdaki bağlantıya tıklayın:</p>
            <p><a href="{verificationLink}">Email Adresimi Doğrula</a></p>
            <p>Bu bağlantı 24 saat geçerlidir.</p>
            """;

        await _emailService.SendEmailAsync(user.Email, "Email Adresinizi Doğrulayın", htmlBody, cancellationToken);

        return new RegisterResponse(user.Id, user.Email);
    }
}
