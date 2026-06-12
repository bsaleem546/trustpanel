using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TrustPanel.Api.HealthChecks;

public sealed class MeilisearchHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public MeilisearchHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var meilisearchUrl = _configuration["MEILISEARCH_URL"];
        if (string.IsNullOrWhiteSpace(meilisearchUrl))
        {
            return HealthCheckResult.Unhealthy("MEILISEARCH_URL is not configured.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(MeilisearchHealthCheck));
            using var response = await client.GetAsync(
                new Uri(new Uri(meilisearchUrl), "/health"),
                cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Meilisearch is reachable.")
                : HealthCheckResult.Unhealthy($"Meilisearch returned status {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Meilisearch is unreachable.", exception);
        }
    }
}
