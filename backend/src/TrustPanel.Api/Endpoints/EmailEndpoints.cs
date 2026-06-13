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
    }
}
