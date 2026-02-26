using Auth.Application.Auth;
using Auth.Application.Login;
using Auth.Application.RefreshToken;
using Auth.Application.Register;
using Auth.Application.ResendVerification;
using Auth.Application.RevokeToken;
using Auth.Application.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Auth.Presentation;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting("fixed");

        group.MapPost("/register", async (RegisterCommand command, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithName("Register")
        .Produces<RegisterResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status409Conflict)
        .RequireRateLimiting("register");

        group.MapPost("/login", async (LoginCommand command, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status401Unauthorized);
            }
        })
        .WithName("Login")
        .Produces<TokenResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .RequireRateLimiting("login");

        group.MapPost("/refresh", async (RefreshTokenCommand command, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status401Unauthorized);
            }
        })
        .WithName("RefreshToken")
        .Produces<TokenResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/revoke", async (RevokeTokenCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result ? Results.Ok() : Results.BadRequest(new { error = "Geçersiz token." });
        })
        .WithName("RevokeToken")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // --- Email Verification Endpoints ---

        group.MapGet("/verify-email", async (string email, string token, IMediator mediator) =>
        {
            var result = await mediator.Send(new VerifyEmailCommand(email, token));
            return result
                ? Results.Ok(new { message = "Email adresiniz başarıyla doğrulandı." })
                : Results.BadRequest(new { error = "Geçersiz veya süresi dolmuş doğrulama bağlantısı." });
        })
        .WithName("VerifyEmail")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/resend-verification", async (ResendVerificationCommand command, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return result
                    ? Results.Ok(new { message = "Doğrulama emaili tekrar gönderildi." })
                    : Results.NotFound(new { error = "Kullanıcı bulunamadı." });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithName("ResendVerification")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .RequireRateLimiting("register");

        return app;
    }
}
