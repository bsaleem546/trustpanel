using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Admin;

namespace TrustPanel.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .RequireAuthorization(SuperAdminPolicy.Name);

        group.MapGet("/workspaces", async (IMediator mediator, int? page, int? pageSize) =>
        {
            var result = await mediator.Send(new ListAdminWorkspacesQuery(page ?? 1, pageSize ?? 50));
            return ApiResults.Ok(result, "Workspaces.");
        });

        group.MapPost("/impersonate", async (
            ImpersonateRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var token = await mediator.Send(
                new ImpersonateUserCommand(user.GetUserId(), request.TargetUserId));
            return ApiResults.Ok(new { token }, "Impersonation token. Valid for one session.");
        });

        group.MapGet("/overrides", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new ListPlanOverridesQuery());
            return ApiResults.Ok(result, "Plan overrides.");
        });

        group.MapPost("/overrides", async (
            CreateOverrideRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreatePlanOverrideCommand(
                user.GetUserId(), request.TargetUserId, request.PlanId,
                request.Reason, request.ExpiresAt));
            return ApiResults.Created(result, "Plan override created.");
        });

        group.MapDelete("/overrides/{overrideId:guid}", async (
            Guid overrideId, IMediator mediator) =>
        {
            await mediator.Send(new DeletePlanOverrideCommand(overrideId));
            return ApiResults.Ok("Override removed.");
        });
    }

    private sealed record ImpersonateRequest(Guid TargetUserId);
    private sealed record CreateOverrideRequest(
        Guid TargetUserId, Guid PlanId, string Reason, DateTimeOffset? ExpiresAt);
}
