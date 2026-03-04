using MediatR;
using User.Domain.Abstractions;

namespace User.Application.Profiles.GetProfile;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileResponse?>
{
    private readonly IProfileRepository _profileRepository;

    public GetProfileQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<ProfileResponse?> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
            return null;

        return new ProfileResponse(
            profile.Id,
            profile.UserId,
            profile.FirstName,
            profile.LastName,
            profile.AvatarUrl);
    }
}
