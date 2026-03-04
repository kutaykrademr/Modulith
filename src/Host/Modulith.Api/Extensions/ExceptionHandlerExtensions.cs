using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Modulith.Api.Extensions;

public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (exceptionFeature is null) return;

                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalExceptionHandler");

                var error = exceptionFeature.Error;
                context.Response.ContentType = "application/problem+json";

                if (error is ValidationException validationEx)
                {
                    var errors = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray());

                    context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

                    await Results.ValidationProblem(
                        errors,
                        statusCode: StatusCodes.Status422UnprocessableEntity,
                        title: "Validation Failed")
                    .ExecuteAsync(context);

                    return;
                }

                var (statusCode, title) = error switch
                {
                    UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                    InvalidOperationException   => (StatusCodes.Status409Conflict, "Conflict"),
                    ArgumentException           => (StatusCodes.Status400BadRequest, "Bad Request"),
                    _                           => (StatusCodes.Status500InternalServerError, "Internal Server Error")
                };

                if (statusCode == StatusCodes.Status500InternalServerError)
                {
                    logger.LogError(error, "Unhandled exception: {Message}", error.Message);
                }

                var isDevelopment = context.RequestServices
                    .GetRequiredService<IWebHostEnvironment>()
                    .IsDevelopment();

                var detail = statusCode == StatusCodes.Status500InternalServerError && !isDevelopment
                    ? "Beklenmeyen bir hata oluştu."
                    : error.Message;

                context.Response.StatusCode = statusCode;

                await Results.Problem(
                    detail: detail,
                    statusCode: statusCode,
                    title: title)
                .ExecuteAsync(context);
            });
        });

        return app;
    }
}
