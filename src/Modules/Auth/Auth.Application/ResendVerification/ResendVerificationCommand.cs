using MediatR;

namespace Auth.Application.ResendVerification;

public sealed record ResendVerificationCommand(string Email) : IRequest<bool>;
