using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Behaviors;
using User.Domain.Abstractions;
using User.Infrastructure.Persistence.Repositories;

namespace User.Infrastructure;

public static class UserModuleServiceRegistration
{
    public static IServiceCollection AddUserModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Repositories
        services.AddScoped<IProfileRepository, ProfileRepository>();

        // Validators
        services.AddValidatorsFromAssembly(typeof(Application.Profiles.GetProfile.GetProfileQuery).Assembly);

        // MediatR + ValidationBehavior
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Application.Profiles.GetProfile.GetProfileQuery).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
