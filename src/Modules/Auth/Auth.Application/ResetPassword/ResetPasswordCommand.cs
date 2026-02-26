using MediatR;

namespace Auth.Application.ResetPassword;

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<bool>;
