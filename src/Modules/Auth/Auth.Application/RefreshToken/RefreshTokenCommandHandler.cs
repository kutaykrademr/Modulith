using Auth.Application.Common;
using Auth.Domain.Abstractions;
using Auth.Domain.Services;
using MediatR;
using Auth.Application.Constants;
using Microsoft.Extensions.Configuration;

namespace Auth.Application.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<TokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Mevcut refresh token'ı özetinden bul
        var tokenHash = TokenHasher.Hash(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null)
        {
            throw new UnauthorizedAccessException(AuthMessages.InvalidOrExpiredRefreshToken);
        }

        // Token reuse detection: zaten revoke edilmiş bir token tekrar sunulduysa, bu büyük
        // ihtimalle çalınmış token'la yapılan bir saldırıdır. Kullanıcının tüm aktif
        // oturumlarını iptal ederek zinciri kır.
        if (existingToken.IsRevoked)
        {
            await _refreshTokenRepository.RevokeAllForUserAsync(existingToken.UserId, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException(AuthMessages.InvalidOrExpiredRefreshToken);
        }

        if (existingToken.IsExpired)
        {
            throw new UnauthorizedAccessException(AuthMessages.InvalidOrExpiredRefreshToken);
        }

        // Eski token'ı revoke et (Token Rotation)
        existingToken.Revoke();

        // Kullanıcıyı bul
        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException(AuthMessages.UserNotFound);
        }

        // Yeni Access + Refresh Token çifti üret
        var refreshExpiryDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = Domain.Entities.RefreshToken.Create(
            TokenHasher.Hash(newRefreshTokenValue),
            user.Id,
            DateTime.UtcNow.AddDays(refreshExpiryDays));

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new TokenResponse(newAccessToken, newRefreshTokenValue, newRefreshToken.ExpiresAtUtc);
    }
}
