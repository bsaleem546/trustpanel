using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TrustPanel.Application.Common;

namespace TrustPanel.Infrastructure.Integrations;

/// <summary>
/// Delivers outbound webhook payloads to registered workspace endpoints.
/// Payload is signed with HMAC-SHA256 using the endpoint's secret.
/// </summary>
public sealed class OutboundWebhookDispatcher
{
    private readonly IAppDbContext _db;
    private readonly HttpClient _http;
    private readonly ILogger<OutboundWebhookDispatcher> _logger;

    public OutboundWebhookDispatcher(
        IAppDbContext db, HttpClient http, ILogger<OutboundWebhookDispatcher> logger)
    {
        _db = db;
        _http = http;
        _logger = logger;
    }

    public async Task DispatchAsync(Guid workspaceId, string eventType, object payload,
        CancellationToken cancellationToken = default)
    {
        var endpoints = await _db.WebhookEndpoints
            .Where(e => e.WorkspaceId == workspaceId && e.IsActive)
            .ToListAsync(cancellationToken);

        if (endpoints.Count == 0) return;

        var body = JsonSerializer.Serialize(new
        {
            @event = eventType,
            data = payload,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });

        foreach (var endpoint in endpoints)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                var signature = ComputeSignature(body, endpoint.Secret);
                request.Headers.Add("X-TrustPanel-Signature", $"sha256={signature}");
                request.Headers.Add("X-TrustPanel-Event", eventType);

                using var response = await _http.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Webhook delivery failed for endpoint {EndpointId}: {StatusCode}",
                        endpoint.Id, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Webhook delivery error for endpoint {EndpointId}.", endpoint.Id);
            }
        }
    }

    public static string ComputeSignature(string body, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(keyBytes, bodyBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
