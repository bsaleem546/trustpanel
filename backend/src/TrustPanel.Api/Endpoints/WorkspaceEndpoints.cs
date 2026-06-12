using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Workspaces;

namespace TrustPanel.Api.Endpoints;

public static class WorkspaceEndpoints
{
    public static void MapWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workspaces").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal user, IMediator mediator) =>
        {
            var workspaces = await mediator.Send(new ListWorkspacesQuery(user.GetUserId()));
            return ApiResults.Ok(
                new { items = workspaces, total = workspaces.Count }, "Workspaces.");
        });

        group.MapPost("/", async (CreateWorkspaceRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var workspace = await mediator.Send(
                new CreateWorkspaceCommand(user.GetUserId(), request.Name));
            return ApiResults.Created(workspace, "Workspace created.");
        });

        group.MapGet("/{workspaceId:guid}", async (Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var workspace = await mediator.Send(
                new GetWorkspaceQuery(user.GetUserId(), workspaceId));
            return ApiResults.Ok(workspace, "Workspace.");
        });

        group.MapPut("/{workspaceId:guid}",
            async (Guid workspaceId, UpdateWorkspaceRequest request, ClaimsPrincipal user, IMediator mediator) =>
            {
                var workspace = await mediator.Send(
                    new UpdateWorkspaceCommand(user.GetUserId(), workspaceId, request.Name));
                return ApiResults.Ok(workspace, "Workspace updated.");
            });

        group.MapDelete("/{workspaceId:guid}", async (Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new DeleteWorkspaceCommand(user.GetUserId(), workspaceId));
            return ApiResults.NoContent("Workspace deleted.");
        });

        group.MapPut("/{workspaceId:guid}/branding",
            async (Guid workspaceId, BrandingRequest request, ClaimsPrincipal user, IMediator mediator) =>
            {
                var workspace = await mediator.Send(new UpdateBrandingCommand(
                    user.GetUserId(),
                    workspaceId,
                    request.LogoPath,
                    request.PrimaryColor,
                    request.SecondaryColor,
                    request.FontFamily,
                    request.ShowTrustPanelBranding,
                    request.EmailFromName,
                    request.EmailFromAddress));
                return ApiResults.Ok(workspace, "Branding updated.");
            });

        group.MapPut("/{workspaceId:guid}/domain",
            async (Guid workspaceId, CustomDomainRequest request, ClaimsPrincipal user, IMediator mediator) =>
            {
                var domain = await mediator.Send(
                    new SetCustomDomainCommand(user.GetUserId(), workspaceId, request.Domain));
                return ApiResults.Ok(domain,
                    $"Custom domain saved. Point a CNAME record at {domain.CnameTarget} to verify.");
            });

        group.MapDelete("/{workspaceId:guid}/domain",
            async (Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
            {
                await mediator.Send(new RemoveCustomDomainCommand(user.GetUserId(), workspaceId));
                return ApiResults.NoContent("Custom domain removed.");
            });

        group.MapPost("/{workspaceId:guid}/domain/verify",
            async (Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
            {
                var domain = await mediator.Send(
                    new VerifyCustomDomainCommand(user.GetUserId(), workspaceId));
                return ApiResults.Ok(domain, domain.Verified
                    ? "Custom domain verified."
                    : "Domain is not verified yet. DNS changes can take up to an hour to propagate.");
            });
    }

    private sealed record CreateWorkspaceRequest(string Name);
    private sealed record UpdateWorkspaceRequest(string Name);
    private sealed record BrandingRequest(
        string? LogoPath,
        string? PrimaryColor,
        string? SecondaryColor,
        string? FontFamily,
        bool? ShowTrustPanelBranding,
        string? EmailFromName,
        string? EmailFromAddress);
    private sealed record CustomDomainRequest(string Domain);
}
