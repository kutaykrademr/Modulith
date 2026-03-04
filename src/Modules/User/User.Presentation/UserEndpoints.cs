using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using User.Application.Profiles.GetProfile;

namespace User.Presentation;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireRateLimiting("fixed");

        group.MapGet("{userId:guid}/profile", async (Guid userId, ISender sender) =>
        {
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
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();
    }
}
