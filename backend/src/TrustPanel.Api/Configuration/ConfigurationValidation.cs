namespace TrustPanel.Api;

public static class ConfigurationValidation
{
    private static readonly string[] RequiredProductionKeys =
    [
        "CONNECTIONSTRINGS__DEFAULT",
        "REDIS_CONNECTION",
        "JWT_SIGNING_KEY",
        "GOOGLE_CLIENT_ID",
        "GOOGLE_CLIENT_SECRET",
        "STRIPE_SECRET_KEY",
        "STRIPE_WEBHOOK_SECRET",
        "RESEND_API_KEY",
        "R2_ACCOUNT_ID",
        "R2_ACCESS_KEY_ID",
        "R2_SECRET_ACCESS_KEY",
        "R2_BUCKET_NAME",
        "TURNSTILE_SECRET_KEY",
        "MEILISEARCH_URL",
        "MEILISEARCH_MASTER_KEY",
        "ANTHROPIC_API_KEY",
        "API_KEY_PEPPER"
    ];

    public static void ValidateRequiredProductionSettings(
        this IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        var missingKeys = RequiredProductionKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .ToArray();

        if (missingKeys.Length > 0)
        {
            throw new InvalidOperationException(
                "Missing required production configuration values: " + string.Join(", ", missingKeys));
        }
    }
}
