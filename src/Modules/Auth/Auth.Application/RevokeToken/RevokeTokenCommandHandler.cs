using Auth.Domain.Abstractions;
using Auth.Domain.Services;
using MediatR;

namespace Auth.Application.RevokeToken;

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, bool>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RevokeTokenCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<bool> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Hash(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (refreshToken is null || !refreshToken.IsActive)
        {
            return false;
        }

        refreshToken.Revoke();
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
