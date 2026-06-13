using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Common;
using TrustPanel.Application.PublicApi;

namespace TrustPanel.Api.Endpoints;

public static class PublicApiV1Endpoints
{
    public static void MapPublicApiV1Endpoints(this IEndpointRouteBuilder app)
    {
        // API key management (dashboard auth required)
        var keyGroup = app.MapGroup("/api/apikeys").RequireAuthorization();

        keyGroup.MapGet("/", async (Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var userId = user.GetUserId();
            var keys = await mediator.Send(new ListApiKeysQuery(userId, workspaceId));
            return ApiResults.Ok(keys, "API keys.");
        });

        keyGroup.MapPost("/", async (CreateKeyRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(
                new CreateApiKeyCommand(user.GetUserId(), request.WorkspaceId, request.Name));
            return ApiResults.Created(new
            {
                key = result.Key,
                plaintextKey = result.PlaintextKey
            }, "API key created. Store the key securely — it will not be shown again.");
        });

        keyGroup.MapPut("/{keyId:guid}/rename", async (
            Guid keyId, RenameKeyRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new RenameApiKeyCommand(user.GetUserId(), request.WorkspaceId, keyId, request.Name));
            return ApiResults.Ok("API key renamed.");
        });

        keyGroup.MapDelete("/{keyId:guid}", async (
            Guid keyId, Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new RevokeApiKeyCommand(user.GetUserId(), workspaceId, keyId));
            return ApiResults.Ok("API key revoked.");
        });

        // v1 public API (API key auth via "ApiKey" scheme)
        var v1 = app.MapGroup("/api/v1")
            .RequireAuthorization(policy => policy.AddAuthenticationSchemes("ApiKey").RequireAuthenticatedUser());

        v1.MapGet("/testimonials", async (
            Guid workspaceId, int? minRating, string? tag, int? page, int? pageSize,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new ListV1TestimonialsQuery(
                workspaceId, minRating, tag, page ?? 1, pageSize ?? 20));
            return ApiResults.Ok(result, "Testimonials.");
        });

        v1.MapGet("/testimonials/{id:guid}", async (
            Guid id, Guid workspaceId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetV1TestimonialQuery(workspaceId, id));
            return ApiResults.Ok(result, "Testimonial.");
        });

        v1.MapPost("/testimonials", async (
            CreateV1Request request, IMediator mediator,
            TrustPanel.Infrastructure.Integrations.OutboundWebhookDispatcher webhooks,
            HttpContext httpContext) =>
        {
            var workspaceId = Guid.TryParse(
                httpContext.User.FindFirst(AppClaims.WorkspaceId)?.Value, out var wsId)
                ? wsId : Guid.Empty;
            var result = await mediator.Send(new CreateV1TestimonialCommand(
                workspaceId, request.Content, request.SubmitterName,
                request.SubmitterEmail, request.Rating));
            await webhooks.DispatchAsync(workspaceId, "testimonial.created", result);
            return ApiResults.Created(result, "Testimonial created.");
        });

        // Webhook endpoint CRUD (dashboard auth)
        var whGroup = app.MapGroup("/api/webhooks").RequireAuthorization();

        whGroup.MapPost("/", async (WebhookCreateRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(
                new CreateWebhookEndpointCommand(user.GetUserId(), request.WorkspaceId, request.Url));
            return ApiResults.Created(result, "Webhook endpoint registered.");
        });

        whGroup.MapDelete("/{endpointId:guid}", async (
            Guid endpointId, Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new DeleteWebhookEndpointCommand(user.GetUserId(), workspaceId, endpointId));
            return ApiResults.Ok("Webhook endpoint removed.");
        });
    }

    private sealed record CreateKeyRequest(Guid WorkspaceId, string Name);
    private sealed record RenameKeyRequest(Guid WorkspaceId, string Name);
    private sealed record CreateV1Request(string Content, string SubmitterName, string? SubmitterEmail, int? Rating);
    private sealed record WebhookCreateRequest(Guid WorkspaceId, string Url);
}
