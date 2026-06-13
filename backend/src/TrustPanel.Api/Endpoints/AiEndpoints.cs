using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Ai;

namespace TrustPanel.Api.Endpoints;

public static class AiEndpoints
{
    public static void MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai").RequireAuthorization();

        group.MapGet("/insights", async (
            Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var report = await mediator.Send(new GetInsightsQuery(user.GetUserId(), workspaceId));
            return report is null
                ? ApiResults.Ok(new { generating = true }, "Insights are being generated.")
                : ApiResults.Ok(report, "Workspace insights.");
        });

        group.MapGet("/reply-suggestion/{testimonialId:guid}", async (
            Guid testimonialId, Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var suggestion = await mediator.Send(
                new GetReplySuggestionQuery(user.GetUserId(), workspaceId, testimonialId));
            return suggestion is null
                ? ApiResults.Ok(new { generating = true }, "Reply suggestion is being generated.")
                : ApiResults.Ok(new { suggestion }, "Reply suggestion.");
        });
    }
}
