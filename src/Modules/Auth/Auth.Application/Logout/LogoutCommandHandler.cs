using Auth.Domain.Abstractions;
using MediatR;

namespace Auth.Application.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _refreshTokenRepository.RevokeAllForUserAsync(request.UserId, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
