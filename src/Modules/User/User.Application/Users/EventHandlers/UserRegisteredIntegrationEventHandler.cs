using Shared.Contracts.IntegrationEvents;
using MediatR;
using User.Domain.Abstractions;
using User.Domain.Entities;

namespace User.Application.Users.EventHandlers;

/// <summary>
/// Auth modülünden gelen UserRegisteredIntegrationEvent'i dinler.
/// Yeni kayıt olan kullanıcı için Profile oluşturur.
/// SaveChanges çağrılmaz; Auth modülündeki UoW (SaveChangesAsync) tüm değişiklikleri tek transaction'da commit eder.
/// </summary>
public sealed class UserRegisteredIntegrationEventHandler
    : INotificationHandler<UserRegisteredIntegrationEvent>
{
    private readonly IProfileRepository _profileRepository;

    public UserRegisteredIntegrationEventHandler(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public Task Handle(UserRegisteredIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // FullName'den FirstName ve LastName ayır
        var nameParts = notification.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
        var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = notification.UserId,
            FirstName = firstName,
            LastName = lastName,
            CreatedAtUtc = DateTime.UtcNow
        };

        _profileRepository.Add(profile);

        return Task.CompletedTask;
    }
}
