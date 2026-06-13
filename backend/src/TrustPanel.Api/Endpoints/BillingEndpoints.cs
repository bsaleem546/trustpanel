using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Billing;

namespace TrustPanel.Api.Endpoints;

public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/billing").RequireAuthorization();

        group.MapPost("/checkout", async (
            CheckoutRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateCheckoutSessionCommand(
                user.GetUserId(), request.PriceId, request.SuccessUrl, request.CancelUrl));
            return ApiResults.Ok(result, "Checkout session created.");
        });

        group.MapPost("/portal", async (
            PortalRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreatePortalSessionCommand(
                user.GetUserId(), request.ReturnUrl));
            return ApiResults.Ok(result, "Portal session created.");
        });

        group.MapGet("/plan", async (Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(
                new TrustPanel.Application.Billing.GetWorkspacePlanQuery(user.GetUserId(), workspaceId));
            return ApiResults.Ok(result, "Current plan.");
        });
    }

    private sealed record CheckoutRequest(string PriceId, string SuccessUrl, string CancelUrl);
    private sealed record PortalRequest(string ReturnUrl);
}
