using Auth.Application.Common;
using Auth.Application.ForgotPassword;
using Auth.Application.Login;
using Auth.Application.Logout;
using Auth.Application.RefreshToken;
using Auth.Application.Register;
using Auth.Application.ResendVerification;
using Auth.Application.ResetPassword;
using Auth.Application.RevokeToken;
using Auth.Application.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace Auth.Presentation;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .RequireRateLimiting("fixed");

        // POST /register → 201 Created (RFC 9110 §9.3.3)
        group.MapPost("/register", async (RegisterCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Created($"/api/v1/users/{result.UserId}/profile", result);
        })
        .WithName("Register")
        .Produces<RegisterResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireRateLimiting("register");

        // POST /login → 200 OK
        group.MapPost("/login", async (LoginCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithName("Login")
        .Produces<TokenResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireRateLimiting("login");

        // POST /refresh → 200 OK
        group.MapPost("/refresh", async (RefreshTokenCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Ok(result);
        })
        .WithName("RefreshToken")
        .Produces<TokenResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        // POST /token/revoke → 204 No Content (RFC 9110 §9.3.4)
        group.MapPost("/token/revoke", async (RevokeTokenCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result
                ? Results.NoContent()
                : Results.Problem(
                    detail: "Geçersiz veya süresi dolmuş token.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request");
        })
        .WithName("RevokeToken")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // POST /logout → 204 No Content (RFC 9110 §9.3.4)
        group.MapPost("/logout", async (ClaimsPrincipal user, IMediator mediator) =>
        {
            var userIdClaim = user.FindFirstValue("sub");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Problem(
                    detail: "Geçersiz veya eksik kullanıcı bilgisi.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized");
            }

            await mediator.Send(new LogoutCommand(userId));
            return Results.NoContent();
        })
        .WithName("Logout")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .RequireAuthorization();

        // GET /email/verify → 200 OK
        group.MapGet("/email/verify", async (string email, string token, IMediator mediator) =>
        {
            var result = await mediator.Send(new VerifyEmailCommand(email, token));
            return result
                ? Results.Ok(new { message = "Email adresiniz başarıyla doğrulandı." })
                : Results.Problem(
                    detail: "Geçersiz veya süresi dolmuş doğrulama bağlantısı.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request");
        })
        .WithName("VerifyEmail")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        // POST /email/verify/send → 202 Accepted (RFC 9110 §15.3.3)
        group.MapPost("/email/verify/send", async (ResendVerificationCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result
                ? Results.Accepted(value: new { message = "Doğrulama emaili tekrar gönderildi." })
                : Results.Problem(
                    detail: "Kullanıcı bulunamadı.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found");
        })
        .WithName("ResendVerification")
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .RequireRateLimiting("register");

        // POST /password/forgot → 202 Accepted (RFC 9110 §15.3.3)
        group.MapPost("/password/forgot", async (ForgotPasswordCommand command, IMediator mediator) =>
        {
            await mediator.Send(command);
            return Results.Accepted(value: new { message = "Şifre sıfırlama bağlantısı email adresinize gönderildi." });
        })
        .WithName("ForgotPassword")
        .Produces(StatusCodes.Status202Accepted)
        .RequireRateLimiting("register");

        // POST /password/reset → 200 OK
        group.MapPost("/password/reset", async (ResetPasswordCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result
                ? Results.Ok(new { message = "Şifreniz başarıyla güncellendi." })
                : Results.Problem(
                    detail: "Geçersiz veya süresi dolmuş sıfırlama bağlantısı.",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request");
        })
        .WithName("ResetPassword")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireRateLimiting("register");

        return app;
    }
}
