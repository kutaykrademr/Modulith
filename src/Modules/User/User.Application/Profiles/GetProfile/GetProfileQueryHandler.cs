using MediatR;
using System.Threading;
using System.Threading.Tasks;
using User.Domain.Abstractions;

namespace User.Application.Profiles.GetProfile;

internal sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    private readonly IProfileRepository _profileRepository;

    public GetProfileQueryHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<ProfileResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        if (profile is null)
        {
            // Throw exception or return null based on your team's convention. For now, returning default or we could make ProfileResponse nullable.
            throw new System.Exception("Profile not found");
        }

        return new ProfileResponse(
            profile.Id,
            profile.UserId,
            profile.FirstName,
            profile.LastName,
            profile.AvatarUrl);
    }
}
