using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        // MediatR - Application katmanindaki handler'lari register et
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(Application.Profiles.GetProfile.GetProfileQuery).Assembly));

        return services;
    }
}
