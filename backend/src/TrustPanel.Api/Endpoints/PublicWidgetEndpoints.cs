using MediatR;
using System.Text.Json;
using TrustPanel.Api.Responses;
using TrustPanel.Application.Common;
using TrustPanel.Application.Widgets;

namespace TrustPanel.Api.Endpoints;

public static class PublicWidgetEndpoints
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    public static void MapPublicWidgetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/widget/{widgetId:guid}", async (
            Guid widgetId,
            IMediator mediator,
            ICacheService cache,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var cacheKey = $"widget:{widgetId}";
            var cached = await cache.GetAsync<PublicWidgetPayload>(cacheKey, cancellationToken);

            if (cached is null)
            {
                cached = await mediator.Send(new GetPublicWidgetQuery(widgetId), cancellationToken);
                await cache.SetAsync(cacheKey, cached, CacheTtl, cancellationToken);
            }

            httpContext.Response.Headers["Cache-Control"] = "public, max-age=60";
            return ApiResults.Ok(cached, "Widget data.");
        });
    }
}
