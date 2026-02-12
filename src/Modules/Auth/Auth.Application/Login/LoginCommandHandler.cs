using Auth.Application.Auth;
using Auth.Domain.Abstractions;
using Auth.Domain.Entities;
using MediatR;

namespace Auth.Application.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<TokenResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Kullanıcıyı bul
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Geçersiz email veya şifre.");
        }

        // Şifreyi doğrula
        var isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Geçersiz email veya şifre.");
        }

        // Access Token üret
        var accessToken = _jwtTokenService.GenerateAccessToken(user);

        // Refresh Token üret ve kaydet
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var refreshToken = Domain.Entities.RefreshToken.Create(
            refreshTokenValue,
            user.Id,
            DateTime.UtcNow.AddDays(7));

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new TokenResponse(accessToken, refreshTokenValue, refreshToken.ExpiresAtUtc);
    }
}
