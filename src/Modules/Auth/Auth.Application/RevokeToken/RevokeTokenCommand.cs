using MediatR;

namespace Auth.Application.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest<bool>;
