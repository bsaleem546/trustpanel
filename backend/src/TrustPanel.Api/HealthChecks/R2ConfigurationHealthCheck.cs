using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TrustPanel.Api.HealthChecks;

public sealed class R2ConfigurationHealthCheck : IHealthCheck
{
    private static readonly string[] RequiredKeys =
    [
        "R2_ACCOUNT_ID",
        "R2_ACCESS_KEY_ID",
        "R2_SECRET_ACCESS_KEY",
        "R2_BUCKET_NAME",
        "R2_PUBLIC_ENDPOINT"
    ];

    private readonly IConfiguration _configuration;

    public R2ConfigurationHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var missingKeys = RequiredKeys
            .Where(key => string.IsNullOrWhiteSpace(_configuration[key]))
            .ToArray();

        return Task.FromResult(missingKeys.Length == 0
            ? HealthCheckResult.Healthy("R2 storage configuration is present.")
            : HealthCheckResult.Unhealthy(
                "Missing R2 storage configuration: " + string.Join(", ", missingKeys)));
    }
}
