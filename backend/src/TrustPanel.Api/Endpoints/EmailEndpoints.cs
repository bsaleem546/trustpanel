using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Email;

namespace TrustPanel.Api.Endpoints;

public static class EmailEndpoints
{
    public static void MapEmailEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/email/unsubscribe", async (
            string token, UnsubscribeService unsubscribe) =>
        {
            var success = await unsubscribe.ProcessUnsubscribeAsync(token, default);
            return success
                ? ApiResults.Ok(new { }, "You have been unsubscribed.")
                : ApiResults.NotFound("Invalid or expired unsubscribe link.");
        });

        app.MapPost("/api/email/request", async (
            SendRequestBody body, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new SendTestimonialRequestCommand(
                user.GetUserId(),
                body.WorkspaceId,
                body.RecipientName,
                body.RecipientEmail,
                body.FormId,
                body.CustomMessage));
            return ApiResults.Ok(new { }, "Request email sent.");
        }).RequireAuthorization();
    }

    private sealed record SendRequestBody(
        Guid WorkspaceId,
        string RecipientName,
        string RecipientEmail,
        string? FormId,
        string? CustomMessage);
}
