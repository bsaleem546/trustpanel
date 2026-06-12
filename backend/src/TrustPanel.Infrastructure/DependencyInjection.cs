using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrustPanel.Application.Auth;
using TrustPanel.Application.Common;
using TrustPanel.Infrastructure.Email;
using TrustPanel.Infrastructure.Identity;
using TrustPanel.Infrastructure.Persistence;
using TrustPanel.Infrastructure.Security;

namespace TrustPanel.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<WorkspaceContext>();
        services.AddScoped<ICurrentWorkspace>(sp => sp.GetRequiredService<WorkspaceContext>());

        var connectionString = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        }

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            // Password reset / email confirmation tokens expire after 60 minutes.
            options.TokenLifespan = TimeSpan.FromMinutes(60);
        });

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IAuthEmailSender, LoggingAuthEmailSender>();

        return services;
    }
}
