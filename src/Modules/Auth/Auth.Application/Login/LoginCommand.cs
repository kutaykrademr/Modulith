using Auth.Application.Auth;
using MediatR;

namespace Auth.Application.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<TokenResponse>;
