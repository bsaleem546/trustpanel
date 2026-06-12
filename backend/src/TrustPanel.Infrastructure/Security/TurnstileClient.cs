using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TrustPanel.Application.Common;

namespace TrustPanel.Infrastructure.Security;

/// <summary>Cloudflare Turnstile siteverify. Pass-through when no secret is configured.</summary>
public sealed class TurnstileClient : ITurnstileVerifier
{
    private const string VerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly HttpClient _httpClient;
    private readonly string? _secretKey;
    private readonly ILogger<TurnstileClient> _logger;

    public TurnstileClient(
        HttpClient httpClient, IConfiguration configuration, ILogger<TurnstileClient> logger)
    {
        _httpClient = httpClient;
        _secretKey = configuration["TURNSTILE_SECRET_KEY"];
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(
        string? token, string? remoteIp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_secretKey))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var payload = new Dictionary<string, string>
            {
                ["secret"] = _secretKey,
                ["response"] = token
            };
            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                payload["remoteip"] = remoteIp;
            }

            using var response = await _httpClient.PostAsync(
                VerifyUrl, new FormUrlEncodedContent(payload), cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>(
                cancellationToken: cancellationToken);
            return result?.Success ?? false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed: an unreachable verifier must not let bots through.
            _logger.LogError(ex, "Turnstile verification request failed.");
            return false;
        }
    }

    private sealed record TurnstileResponse(bool Success);
}
