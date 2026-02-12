namespace Auth.Application.Auth;

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);
