using MediatR;
using System;

namespace User.Application.Profiles.GetProfile;

public sealed record GetProfileQuery(Guid UserId) : IRequest<ProfileResponse>;

public sealed record ProfileResponse(Guid Id, Guid UserId, string FirstName, string LastName, string? AvatarUrl);
