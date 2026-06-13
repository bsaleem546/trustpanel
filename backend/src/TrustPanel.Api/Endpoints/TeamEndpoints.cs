using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Teams;
using TrustPanel.Domain.Teams;

namespace TrustPanel.Api.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/team").RequireAuthorization();

        group.MapGet("/", async (
            Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var members = await mediator.Send(
                new ListTeamMembersQuery(user.GetUserId(), workspaceId));
            return ApiResults.Ok(members, "Team members.");
        });

        group.MapPost("/invite", async (
            InviteRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var token = await mediator.Send(
                new InviteMemberCommand(user.GetUserId(), request.WorkspaceId,
                    request.Email, request.Role));
            return ApiResults.Ok(new { token }, "Invitation sent.");
        });

        group.MapPost("/accept", async (
            AcceptInviteRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                     ?? user.FindFirst("email")?.Value
                     ?? string.Empty;
            var workspaceId = await mediator.Send(
                new AcceptInvitationCommand(request.Token, user.GetUserId(), email));
            return ApiResults.Ok(new { workspaceId }, "Invitation accepted.");
        });

        group.MapPut("/{memberId:guid}/role", async (
            Guid memberId, ChangeRoleRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new ChangeMemberRoleCommand(
                user.GetUserId(), request.WorkspaceId, memberId, request.Role));
            return ApiResults.Ok("Role updated.");
        });

        group.MapDelete("/{memberId:guid}", async (
            Guid memberId, Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new RemoveMemberCommand(user.GetUserId(), workspaceId, memberId));
            return ApiResults.Ok("Member removed.");
        });
    }

    private sealed record InviteRequest(Guid WorkspaceId, string Email, WorkspaceRole Role);
    private sealed record AcceptInviteRequest(string Token);
    private sealed record ChangeRoleRequest(Guid WorkspaceId, WorkspaceRole Role);
}
