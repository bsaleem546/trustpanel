using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Widgets;
using TrustPanel.Domain.Testimonials;
using TrustPanel.Domain.Widgets;

namespace TrustPanel.Api.Endpoints;

public static class WidgetEndpoints
{
    public static void MapWidgetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/widgets").RequireAuthorization();

        group.MapGet("/", async (Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var widgets = await mediator.Send(new ListWidgetsQuery(user.GetUserId(), workspaceId));
            return ApiResults.Ok(new { items = widgets, total = widgets.Count }, "Widgets.");
        });

        group.MapGet("/{widgetId:guid}", async (Guid widgetId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var widget = await mediator.Send(new GetWidgetQuery(user.GetUserId(), widgetId));
            return ApiResults.Ok(widget, "Widget.");
        });

        group.MapPost("/", async (WidgetRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var widget = await mediator.Send(new CreateWidgetCommand(
                user.GetUserId(), request.WorkspaceId, request.ToPayload()));
            return ApiResults.Created(widget, "Widget created.");
        });

        group.MapPut("/{widgetId:guid}", async (
            Guid widgetId, WidgetRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var widget = await mediator.Send(new UpdateWidgetCommand(
                user.GetUserId(), widgetId, request.ToPayload()));
            return ApiResults.Ok(widget, "Widget updated.");
        });

        group.MapDelete("/{widgetId:guid}", async (
            Guid widgetId, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new DeleteWidgetCommand(user.GetUserId(), widgetId));
            return ApiResults.Ok(new { }, "Widget deleted.");
        });
    }

    private sealed record WidgetRequest(
        Guid WorkspaceId,
        WidgetType Type,
        string Name,
        IReadOnlyList<string>? FilterTags,
        int? MinimumRating,
        bool? FeaturedOnly,
        IReadOnlyList<Guid>? SelectedTestimonialIds,
        TestimonialSource? SourceFilter,
        WidgetSettings? Settings,
        string? CustomCss)
    {
        public WidgetPayload ToPayload() => new(
            Type, Name, FilterTags, MinimumRating, FeaturedOnly,
            SelectedTestimonialIds, SourceFilter, Settings, CustomCss);
    }
}
