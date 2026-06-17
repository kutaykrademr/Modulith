using Auth.Domain.Abstractions;
using MediatR;

namespace Auth.Application.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
        if (user is null)
            return false;

        // Yeni şifreyi hashle
        var newPasswordHash = _passwordHasher.Hash(request.NewPassword);

        // Token doğrulama + şifre güncelleme (domain logic)
        var result = user.ResetPassword(request.Token, newPasswordHash);

        if (result)
        {
            // Güvenlik: şifre değiştiğinde mevcut tüm refresh token'ları (oturumları) iptal et.
            await _refreshTokenRepository.RevokeAllForUserAsync(user.Id, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
