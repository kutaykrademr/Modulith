using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Modulith.Api.Extensions.Transformers;

internal sealed class BearerSecurityOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var endpointMetadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (endpointMetadata is null)
        {
            return Task.CompletedTask;
        }

        var hasAllowAnonymous = endpointMetadata.OfType<IAllowAnonymous>().Any();
        if (hasAllowAnonymous)
        {
            return Task.CompletedTask;
        }

        var hasAuthorize = endpointMetadata.OfType<IAuthorizeData>().Any();
        if (!hasAuthorize)
        {
            return Task.CompletedTask;
        }

        var document = context.Document;
        if (document is null)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();

        var securityRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        };

        operation.Security.Add(securityRequirement);

        return Task.CompletedTask;
    }
}
