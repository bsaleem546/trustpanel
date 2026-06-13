using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Amazon.Runtime;
using Amazon.S3;
using Meilisearch;
using TrustPanel.Application.Ai;
using TrustPanel.Application.Auth;
using TrustPanel.Application.Billing;
using TrustPanel.Application.Common;
using TrustPanel.Application.Forms;
using TrustPanel.Application.Workspaces;
using TrustPanel.Application.Common;
using TrustPanel.Application.Uploads;
using TrustPanel.Infrastructure.Billing;
using TrustPanel.Infrastructure.Caching;
using TrustPanel.Infrastructure.Email;
using TrustPanel.Infrastructure.Identity;
using TrustPanel.Infrastructure.Jobs;
using TrustPanel.Infrastructure.Persistence;
using TrustPanel.Infrastructure.RateLimiting;
using TrustPanel.Infrastructure.Search;
using TrustPanel.Infrastructure.Security;
using TrustPanel.Infrastructure.Storage;

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
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseNpgsql(connectionString);
                var interceptor = sp.GetService<SearchIndexSaveChangesInterceptor>();
                if (interceptor is not null)
                    options.AddInterceptors(interceptor);
            });
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

        services.AddScoped<IPlanResolver, PlanResolver>();
        services.AddSingleton<IDnsResolver, DnsClientResolver>();
        services.AddSingleton(new CustomDomainOptions(
            configuration["CUSTOM_DOMAIN_CNAME_TARGET"] ?? "domains.trustpanel.com"));
        services.AddScoped<VerifyWorkspaceDomainJob>();

        var redisConnection = configuration["REDIS_CONNECTION"];
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(redisConnection));
            services.AddSingleton<IRateLimiter, RedisRateLimiter>();
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
            services.AddSingleton<ICacheService, InMemoryCacheService>();
        }

        services.AddHttpClient<ITurnstileVerifier, TurnstileClient>();
        services.AddSingleton(new SubmissionOptions(
            int.TryParse(configuration["SUBMISSION_RATE_LIMIT_PER_HOUR"], out var perHour)
                ? perHour
                : 5));
        services.AddSingleton<ISubmissionJobDispatcher, SubmissionJobDispatcher>();
        services.AddScoped<IAiService, NullAiService>();
        services.AddScoped<SendTestimonialThankYouJob>();
        services.AddScoped<NotifyWorkspaceOwnerJob>();
        services.AddScoped<AnalyzeTestimonialSentimentJob>();
        services.AddScoped<ImportTestimonialsCsvJob>();

        var meilisearchUrl = configuration["MEILISEARCH_URL"];
        if (!string.IsNullOrWhiteSpace(meilisearchUrl))
        {
            services.AddSingleton(_ => new MeilisearchClient(
                meilisearchUrl, configuration["MEILISEARCH_API_KEY"]));
            services.AddSingleton<ISearchIndexer, MeilisearchTestimonialIndexer>();
        }
        else
        {
            services.AddSingleton<ISearchIndexer, NullSearchIndexer>();
        }

        services.AddScoped<SearchIndexSaveChangesInterceptor>();

        var r2AccountId = configuration["R2_ACCOUNT_ID"];
        if (!string.IsNullOrWhiteSpace(r2AccountId))
        {
            services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                new BasicAWSCredentials(
                    configuration["R2_ACCESS_KEY_ID"],
                    configuration["R2_SECRET_ACCESS_KEY"]),
                new AmazonS3Config
                {
                    ServiceURL = $"https://{r2AccountId}.r2.cloudflarestorage.com",
                    ForcePathStyle = true
                }));
            services.AddSingleton<IStorageService, R2StorageService>();
        }
        else
        {
            services.AddSingleton<IStorageService, NullStorageService>();
        }

        services.AddScoped<ProcessVideoJob>();

        if (!string.IsNullOrWhiteSpace(configuration["STRIPE_SECRET_KEY"]))
            services.AddScoped<IBillingService, StripeBillingService>();
        else
            services.AddScoped<IBillingService, NullBillingService>();

        return services;
    }
}
