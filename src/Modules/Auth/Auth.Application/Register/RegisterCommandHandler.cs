using Auth.Domain.Abstractions;
using Auth.Domain.Entities;
using Shared.Contracts.IntegrationEvents;
using Shared.Contracts.Abstractions;
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
    private readonly IPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        IConfiguration configuration,
        IPublisher publisher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _configuration = configuration;
        _publisher = publisher;
        _unitOfWork = unitOfWork;
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

        // UoW: Transaction başlat — tüm modüllerin değişiklikleri atomik olacak
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Auth User'ı ekle
            await _userRepository.AddAsync(user, cancellationToken);

            // Integration event yayınla — User modülü bu event'i dinleyerek Profile oluşturur
            await _publisher.Publish(
                new UserRegisteredIntegrationEvent(user.Id, user.Email, user.FullName),
                cancellationToken);

            // Tüm modüllerin değişikliklerini kaydet
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Transaction'ı commit et
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            // Herhangi bir hata olursa tüm değişiklikleri geri al
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        // Doğrulama emaili gönder (transaction dışında — email gönderimi DB ile ilgili değil)
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
