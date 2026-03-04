using MediatR;

namespace Shared.Contracts.IntegrationEvents;

/// <summary>
/// Auth modülünde yeni bir kullanıcı kayıt olduğunda yayınlanan integration event.
/// Diğer modüller (User, Profile vb.) bu event'i dinleyerek kendi kayıtlarını oluşturur.
/// Shared.Contracts'ta tanımlanır — modüller birbirini değil, bu projeyi referans alır.
/// </summary>
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string FullName) : INotification;
