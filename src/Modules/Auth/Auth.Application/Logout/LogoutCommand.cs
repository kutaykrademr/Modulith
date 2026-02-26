using MediatR;

namespace Auth.Application.Logout;

public sealed record LogoutCommand(Guid UserId) : IRequest<bool>;
