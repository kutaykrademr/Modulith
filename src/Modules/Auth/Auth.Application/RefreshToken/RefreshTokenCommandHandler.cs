using Auth.Application.Auth;
using Auth.Domain.Abstractions;
using MediatR;

namespace Auth.Application.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<TokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Mevcut refresh token'ı bul
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (existingToken is null || !existingToken.IsActive)
        {
            throw new UnauthorizedAccessException("Geçersiz veya süresi dolmuş refresh token.");
        }

        // Eski token'ı revoke et (Token Rotation)
        existingToken.Revoke();

        // Kullanıcıyı bul
        var user = await _userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Kullanıcı bulunamadı.");
        }

        // Yeni Access + Refresh Token çifti üret
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = Domain.Entities.RefreshToken.Create(
            newRefreshTokenValue,
            user.Id,
            DateTime.UtcNow.AddDays(7));

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new TokenResponse(newAccessToken, newRefreshTokenValue, newRefreshToken.ExpiresAtUtc);
    }
}
