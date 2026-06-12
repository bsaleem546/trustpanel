using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TrustPanel.Application.Auth;
using TrustPanel.Application.Common.Behaviors;

namespace TrustPanel.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<AuthSessionService>();

        return services;
    }
}
