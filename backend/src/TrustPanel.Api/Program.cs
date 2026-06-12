using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TrustPanel.Api;
using TrustPanel.Api.HealthChecks;
using TrustPanel.Api.Middleware;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Common;
using TrustPanel.Infrastructure;

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

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

var dataProtection = builder.Services.AddDataProtection();
var dataProtectionKeysPath = builder.Configuration["DATA_PROTECTION_KEYS_PATH"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

var healthChecks = builder.Services.AddHealthChecks();
var defaultConnectionString = builder.Configuration.GetConnectionString("Default");
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
app.UseMiddleware<WorkspaceResolutionMiddleware>();

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
}

app.Run();

public partial class Program
{
}
