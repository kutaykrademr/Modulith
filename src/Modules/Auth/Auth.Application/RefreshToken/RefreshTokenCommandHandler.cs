using Auth.Application.Common;
using Auth.Domain.Abstractions;
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
        // Mevcut refresh token'ı bul
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (existingToken is null || !existingToken.IsActive)
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
            newRefreshTokenValue,
            user.Id,
            DateTime.UtcNow.AddDays(refreshExpiryDays));

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new TokenResponse(newAccessToken, newRefreshTokenValue, newRefreshToken.ExpiresAtUtc);
    }
}
