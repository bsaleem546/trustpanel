using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TrustPanel.Api.Responses;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Email;

namespace TrustPanel.Api.Endpoints;

public static class ResendWebhookEndpoints
{
    public static void MapResendWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/resend", async (HttpContext httpContext, IAppDbContext db) =>
        {
            string payload;
            using (var reader = new StreamReader(httpContext.Request.Body))
            {
                payload = await reader.ReadToEndAsync();
            }

            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                var eventType = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
                var messageId = root.TryGetProperty("data", out var dataProp)
                    && dataProp.TryGetProperty("email_id", out var idProp)
                    ? idProp.GetString()
                    : null;

                if (messageId is not null)
                {
                    var log = await db.EmailLogs
                        .FirstOrDefaultAsync(l => l.ProviderMessageId == messageId);

                    if (log is not null)
                    {
                        log.Status = eventType switch
                        {
                            "email.delivered" => EmailStatus.Delivered,
                            "email.bounced" => EmailStatus.Bounced,
                            "email.complained" => EmailStatus.Complained,
                            _ => log.Status
                        };

                        if (eventType == "email.delivered") log.DeliveredAt = DateTimeOffset.UtcNow;
                        if (eventType is "email.bounced" or "email.complained") log.FailedAt = DateTimeOffset.UtcNow;

                        await db.SaveChangesAsync(httpContext.RequestAborted);
                    }
                }
            }
            catch
            {
                // Best-effort — don't fail Resend retries on parse errors.
            }

            return ApiResults.Ok(new { received = true }, "Webhook processed.");
        });
    }
}
