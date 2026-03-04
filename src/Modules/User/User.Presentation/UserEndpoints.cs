using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using User.Application.Profiles.GetProfile;

namespace User.Presentation;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireRateLimiting("fixed");

        group.MapGet("{userId:guid}/profile", async (Guid userId, ClaimsPrincipal user, ISender sender) =>
        {
            var subClaim = user.FindFirstValue("sub");
            if (subClaim is null || !Guid.TryParse(subClaim, out var requesterId) || requesterId != userId)
            {
                return Results.Problem(
                    detail: "Başka bir kullanıcının profiline erişemezsiniz.",
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden");
            }

            var query = new GetProfileQuery(userId);
            var result = await sender.Send(query);

            return result is not null
                ? Results.Ok(result)
                : Results.Problem(
                    detail: "Profil bulunamadı.",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Not Found");
        })
        .WithName("GetProfile")
        .Produces<ProfileResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
