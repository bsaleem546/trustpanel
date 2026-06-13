using Microsoft.EntityFrameworkCore;
using TrustPanel.Api.Responses;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Widgets;

namespace TrustPanel.Api.Endpoints;

public static class PublicEventEndpoints
{
    public static void MapPublicEventEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/public/events", async (
            EventPayload payload,
            HttpContext httpContext,
            IAppDbContext db,
            IRateLimiter rateLimiter,
            CancellationToken cancellationToken) =>
        {
            var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            var allowed = await rateLimiter.TryConsumeAsync(
                $"event:{clientIp}", 100, TimeSpan.FromMinutes(1), cancellationToken);
            if (!allowed)
                return ApiResults.RateLimited("Too many events. Please slow down.");

            var widget = await db.Widgets
                .FirstOrDefaultAsync(w => w.Id == payload.WidgetId, cancellationToken);
            if (widget is null)
                return ApiResults.NotFound("Widget not found.");

            db.WidgetEvents.Add(new WidgetEvent
            {
                WidgetId = payload.WidgetId,
                WorkspaceId = widget.WorkspaceId,
                TestimonialId = payload.TestimonialId,
                Event = payload.Event,
                Country = payload.Country,
                Device = payload.Device,
                Referrer = payload.Referrer,
                OccurredAt = DateTimeOffset.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
            return ApiResults.Ok(new { }, "Event recorded.");
        });
    }

    private sealed record EventPayload(
        Guid WidgetId,
        WidgetEventType Event,
        Guid? TestimonialId,
        string? Country,
        string? Device,
        string? Referrer);
}
