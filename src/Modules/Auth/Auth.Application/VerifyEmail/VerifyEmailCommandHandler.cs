using Auth.Domain.Abstractions;
using MediatR;

namespace Auth.Application.VerifyEmail;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, bool>
{
    private readonly IUserRepository _userRepository;

    public VerifyEmailCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant(), cancellationToken);
        if (user is null)
            return false;

        var result = user.VerifyEmail(request.Token);
        if (!result)
            return false;

        await _userRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
