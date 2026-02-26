using MediatR;

namespace Auth.Application.VerifyEmail;

public sealed record VerifyEmailCommand(string Email, string Token) : IRequest<bool>;
