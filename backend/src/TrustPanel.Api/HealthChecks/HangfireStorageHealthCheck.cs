using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace TrustPanel.Api.HealthChecks;

public sealed class HangfireStorageHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public HangfireStorageHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var storage = _configuration["HANGFIRE_STORAGE"];
        if (string.IsNullOrWhiteSpace(storage))
        {
            return HealthCheckResult.Unhealthy("HANGFIRE_STORAGE is not configured.");
        }

        if (!string.Equals(storage, "postgres", StringComparison.OrdinalIgnoreCase))
        {
            return HealthCheckResult.Unhealthy($"Unsupported Hangfire storage '{storage}'.");
        }

        var connectionString = _configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("Hangfire storage connection string is not configured.");
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy("Hangfire PostgreSQL storage is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Hangfire PostgreSQL storage is unreachable.", exception);
        }
    }
}
