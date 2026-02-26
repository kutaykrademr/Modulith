using MediatR;

namespace Auth.Application.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<bool>;
