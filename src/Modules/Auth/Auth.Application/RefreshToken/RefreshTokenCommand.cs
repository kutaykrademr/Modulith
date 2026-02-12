using Auth.Application.Auth;
using MediatR;

namespace Auth.Application.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<TokenResponse>;
