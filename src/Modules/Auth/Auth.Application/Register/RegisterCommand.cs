using MediatR;

namespace Auth.Application.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName) : IRequest<RegisterResponse>;
