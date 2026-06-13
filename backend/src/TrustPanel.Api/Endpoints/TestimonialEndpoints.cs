using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Common;
using TrustPanel.Application.Integrations;
using TrustPanel.Application.Testimonials;
using TrustPanel.Domain.Integrations;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Api.Endpoints;

public static class TestimonialEndpoints
{
    public static void MapTestimonialEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/testimonials").RequireAuthorization();

        // ── List / search ─────────────────────────────────────────────────────

        group.MapGet("/", async (
            Guid workspaceId,
            TestimonialStatus? status,
            string? tag,
            int page,
            int pageSize,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new ListTestimonialsQuery(
                user.GetUserId(), workspaceId, status, tag,
                page < 1 ? 1 : page,
                pageSize < 1 ? 25 : pageSize));
            return ApiResults.Ok(result, "Testimonials.");
        });

        group.MapGet("/search", async (
            Guid workspaceId,
            string q,
            int limit,
            ClaimsPrincipal user,
            IMediator mediator) =>
        {
            var results = await mediator.Send(new SearchTestimonialsQuery(
                user.GetUserId(), workspaceId, q, limit < 1 ? 25 : limit));
            return ApiResults.Ok(new { items = results, total = results.Count }, "Search results.");
        });

        group.MapGet("/{testimonialId:guid}", async (
            Guid testimonialId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var t = await mediator.Send(new GetTestimonialQuery(user.GetUserId(), testimonialId));
            return ApiResults.Ok(t, "Testimonial.");
        });

        // ── Moderation ────────────────────────────────────────────────────────

        group.MapPost("/{testimonialId:guid}/approve", async (
            Guid testimonialId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var t = await mediator.Send(new ApproveTestimonialCommand(user.GetUserId(), testimonialId));
            return ApiResults.Ok(t, "Testimonial approved.");
        });

        group.MapPost("/{testimonialId:guid}/reject", async (
            Guid testimonialId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var t = await mediator.Send(new RejectTestimonialCommand(user.GetUserId(), testimonialId));
            return ApiResults.Ok(t, "Testimonial rejected.");
        });

        group.MapPost("/{testimonialId:guid}/feature", async (
            Guid testimonialId, bool featured, ClaimsPrincipal user, IMediator mediator) =>
        {
            var t = await mediator.Send(new FeatureTestimonialCommand(user.GetUserId(), testimonialId, featured));
            return ApiResults.Ok(t, featured ? "Testimonial featured." : "Testimonial unfeatured.");
        });

        group.MapPut("/{testimonialId:guid}/tags", async (
            Guid testimonialId, UpdateTagsRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var t = await mediator.Send(new UpdateTestimonialTagsCommand(
                user.GetUserId(), testimonialId, request.Tags));
            return ApiResults.Ok(t, "Tags updated.");
        });

        group.MapPut("/{testimonialId:guid}", async (
            Guid testimonialId, EditTestimonialRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var t = await mediator.Send(new EditTestimonialCommand(
                user.GetUserId(), testimonialId, request.Content, request.Rating));
            return ApiResults.Ok(t, "Testimonial updated.");
        });

        group.MapDelete("/{testimonialId:guid}", async (
            Guid testimonialId, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new DeleteTestimonialCommand(user.GetUserId(), testimonialId));
            return ApiResults.NoContent("Testimonial deleted.");
        });

        // ── Batch ─────────────────────────────────────────────────────────────

        group.MapPost("/batch", async (
            BatchRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var count = await mediator.Send(new BatchTestimonialCommand(
                user.GetUserId(), request.WorkspaceId, request.TestimonialIds, request.Action));
            return ApiResults.Ok(new { affected = count }, $"Batch {request.Action} complete.");
        });

        // ── Import sources ────────────────────────────────────────────────────

        group.MapGet("/import-sources", async (
            Guid workspaceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var sources = await mediator.Send(new ListImportSourcesQuery(user.GetUserId(), workspaceId));
            return ApiResults.Ok(new { items = sources, total = sources.Count }, "Import sources.");
        });

        group.MapPost("/import-sources", async (
            CreateImportSourceRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var source = await mediator.Send(new CreateImportSourceCommand(
                user.GetUserId(), request.WorkspaceId, request.Provider, request.ExternalAccountId));
            return ApiResults.Created(source, "Import source created.");
        });

        group.MapDelete("/import-sources/{sourceId:guid}", async (
            Guid sourceId, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new DeleteImportSourceCommand(user.GetUserId(), sourceId));
            return ApiResults.NoContent("Import source deleted.");
        });
    }

    private sealed record UpdateTagsRequest(IReadOnlyList<string> Tags);
    private sealed record EditTestimonialRequest(string Content, int? Rating);
    private sealed record BatchRequest(
        Guid WorkspaceId,
        IReadOnlyList<Guid> TestimonialIds,
        BatchTestimonialAction Action);
    private sealed record CreateImportSourceRequest(
        Guid WorkspaceId,
        ImportProvider Provider,
        string? ExternalAccountId);
}
