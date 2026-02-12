using Auth.Domain.Entities;

namespace Auth.Domain.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
