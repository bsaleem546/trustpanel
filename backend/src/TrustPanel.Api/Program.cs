using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TrustPanel.Api;
using TrustPanel.Api.Endpoints;
using TrustPanel.Api.HealthChecks;
using TrustPanel.Api.Middleware;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application;
using TrustPanel.Application.Common;
using TrustPanel.Infrastructure;
using TrustPanel.Infrastructure.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
});

var sentryDsn = builder.Configuration["SENTRY_DSN"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Release = builder.Configuration["SENTRY_RELEASE"];
        options.Environment = builder.Environment.EnvironmentName;
        options.TracesSampleRate = 0.1;
    });
}

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

var jwtOptions = JwtOptions.From(builder.Configuration);
var authBuilder = builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddScheme<TrustPanel.Api.Security.ApiKeyAuthenticationOptions, TrustPanel.Api.Security.ApiKeyAuthenticationHandler>(
        "ApiKey", _ => { })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = jwtOptions.ToTokenValidationParameters();
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await ApiResults.Unauthorized().ExecuteAsync(context.HttpContext);
            },
            OnForbidden = context => ApiResults.Forbidden().ExecuteAsync(context.HttpContext)
        };
    });

var googleClientId = builder.Configuration["GOOGLE_CLIENT_ID"];
if (!string.IsNullOrWhiteSpace(googleClientId))
{
    authBuilder
        .AddCookie("External")
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? string.Empty;
            options.SignInScheme = "External";
        });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(TrustPanel.Api.Security.SuperAdminPolicy.Name,
        TrustPanel.Api.Security.SuperAdminPolicy.Build());
});

var dataProtection = builder.Services.AddDataProtection();
var dataProtectionKeysPath = builder.Configuration["DATA_PROTECTION_KEYS_PATH"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

var defaultConnectionString = builder.Configuration.GetConnectionString("Default");

// Hangfire owns all slow work. The server is skipped in tests so WebApplicationFactory
// boots without polling job storage; IJobScheduler then degrades to a logged no-op.
var hangfireEnabled = !string.IsNullOrWhiteSpace(defaultConnectionString)
    && builder.Environment.EnvironmentName != "Testing";
if (hangfireEnabled)
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(defaultConnectionString)));
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = int.TryParse(builder.Configuration["HANGFIRE_WORKER_COUNT"], out var workers)
            ? workers
            : 5;
    });
    builder.Services.AddScoped<IJobScheduler, HangfireJobScheduler>();
}
else
{
    builder.Services.AddScoped<IJobScheduler, NullJobScheduler>();
}

var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(defaultConnectionString))
{
    healthChecks.AddNpgSql(defaultConnectionString, name: "postgresql", tags: ["ready"]);
    healthChecks.AddCheck<HangfireStorageHealthCheck>("hangfire-storage", tags: ["ready"]);
}

var redisConnection = builder.Configuration["REDIS_CONNECTION"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    healthChecks.AddRedis(redisConnection, name: "redis", tags: ["ready"]);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["MEILISEARCH_URL"]))
{
    healthChecks.AddCheck<MeilisearchHealthCheck>("meilisearch", tags: ["ready"]);
}

if (!string.IsNullOrWhiteSpace(builder.Configuration["R2_ACCOUNT_ID"]))
{
    healthChecks.AddCheck<R2ConfigurationHealthCheck>("r2-configuration", tags: ["ready"]);
}

builder.Configuration.ValidateRequiredProductionSettings(builder.Environment);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<WorkspaceResolutionMiddleware>();

app.MapAuthEndpoints();
app.MapWorkspaceEndpoints();
app.MapFormEndpoints();
app.MapPublicFormEndpoints();
app.MapTestimonialEndpoints();
app.MapUploadEndpoints();
app.MapWidgetEndpoints();
app.MapPublicWidgetEndpoints();
app.MapBillingEndpoints();
app.MapStripeWebhookEndpoints();
app.MapEmailEndpoints();
app.MapResendWebhookEndpoints();
app.MapAnalyticsEndpoints();
app.MapAiEndpoints();
app.MapTeamEndpoints();
app.MapPublicApiV1Endpoints();
app.MapAdminEndpoints();
app.MapGdprEndpoints();
app.MapPublicEventEndpoints();

if (hangfireEnabled)
{
    using var scope = app.Services.CreateScope();
    var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<VerifyWorkspaceDomainJob>(
        "verify-workspace-domains",
        job => job.RunAsync(CancellationToken.None),
        Cron.Hourly());
    recurringJobs.AddOrUpdate<AggregateWidgetAnalyticsJob>(
        "aggregate-widget-analytics",
        job => job.RunAsync(CancellationToken.None),
        Cron.Daily());
}

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Testing")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => ApiResults.Ok(new { service = "TrustPanel API" }, "TrustPanel API is running."));

app.MapGet("/health", () => ApiResults.Ok(new
{
    service = "TrustPanel API",
    status = "healthy",
    checkedAt = DateTimeOffset.UtcNow
}, "TrustPanel API is healthy."));

app.MapGet("/health/ready", async (HealthCheckService healthCheckService, CancellationToken cancellationToken) =>
{
    var report = await healthCheckService.CheckHealthAsync(
        registration => registration.Tags.Contains("ready"),
        cancellationToken);

    var data = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.ToDictionary(
            entry => entry.Key,
            entry => new
            {
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds
            })
    };

    return report.Status == HealthStatus.Healthy
        ? ApiResults.Ok(data, "All readiness checks passed.")
        : ApiResults.Envelope(
            StatusCodes.Status503ServiceUnavailable,
            false,
            data,
            "One or more readiness checks failed.",
            "Service unavailable.",
            new Dictionary<string, string[]>());
});

if (app.Environment.EnvironmentName == "Testing")
{
    app.MapGet("/api/_diagnostics/boom", () =>
    {
        throw new InvalidOperationException("Diagnostics exception.");
    });

    // Exposes the workspace resolved by WorkspaceResolutionMiddleware so tests can
    // assert host-based (custom domain) resolution on public paths.
    app.MapGet("/api/public/_diagnostics/workspace", (ICurrentWorkspace workspace) =>
        ApiResults.Ok(new { workspaceId = workspace.WorkspaceId }, "Resolved workspace."));
}

app.Run();

public partial class Program
{
}
