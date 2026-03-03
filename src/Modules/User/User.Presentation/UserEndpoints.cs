using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using User.Application.Profiles.GetProfile;
using System;

namespace User.Presentation;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/users")
            .WithTags("Users");

        group.MapGet("{userId:guid}/profile", async (Guid userId, ISender sender) =>
        {
            try
            {
                var query = new GetProfileQuery(userId);
                var result = await sender.Send(query);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                // In a real scenario, use a global exception handler.
                return Results.NotFound(new { Message = ex.Message });
            }
        }).RequireAuthorization();
    }
}
