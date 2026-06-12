using MediatR;
using TrustPanel.Api.Responses;
using TrustPanel.Application.Common;
using TrustPanel.Application.Forms;

namespace TrustPanel.Api.Endpoints;

public static class PublicFormEndpoints
{
    public static void MapPublicFormEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/public/forms").AddEndpointFilter(CspHeaderFilter);

        // Host-resolved (verified custom domain) routes.
        group.MapGet("/{formSlug}", async (
            string formSlug, ICurrentWorkspace workspace, IMediator mediator) =>
        {
            var form = await mediator.Send(new GetPublicFormQuery(
                RequireHostWorkspace(workspace), null, formSlug));
            return ApiResults.Ok(form, "Collection form.");
        });

        group.MapPost("/{formSlug}/submissions", async (
            string formSlug, SubmissionRequest request, HttpContext context,
            ICurrentWorkspace workspace, IMediator mediator) =>
        {
            var result = await mediator.Send(request.ToCommand(
                RequireHostWorkspace(workspace), null, formSlug, ClientIp(context)));
            return ApiResults.Created(result, "Thank you! Your testimonial has been submitted.");
        });

        // Workspace-slug routes (default trustpanel-hosted collection pages).
        group.MapGet("/{workspaceSlug}/{formSlug}", async (
            string workspaceSlug, string formSlug, IMediator mediator) =>
        {
            var form = await mediator.Send(new GetPublicFormQuery(null, workspaceSlug, formSlug));
            return ApiResults.Ok(form, "Collection form.");
        });

        group.MapPost("/{workspaceSlug}/{formSlug}/submissions", async (
            string workspaceSlug, string formSlug, SubmissionRequest request,
            HttpContext context, IMediator mediator) =>
        {
            var result = await mediator.Send(request.ToCommand(
                null, workspaceSlug, formSlug, ClientIp(context)));
            return ApiResults.Created(result, "Thank you! Your testimonial has been submitted.");
        });
    }

    private static Guid RequireHostWorkspace(ICurrentWorkspace workspace)
        => workspace.WorkspaceId ?? throw new NotFoundException("Form not found.");

    /// <summary>Client IP for rate limiting: first X-Forwarded-For hop, else socket address.</summary>
    internal static string ClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>CSP for public collection endpoints rendered inside customer pages.</summary>
    internal static async ValueTask<object?> CspHeaderFilter(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        context.HttpContext.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; img-src * data:; media-src *; frame-ancestors *";
        context.HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        return await next(context);
    }

    private sealed record SubmissionRequest(
        string? TurnstileToken,
        string Content,
        int? Rating,
        string Name,
        string? Email,
        string? Company,
        string? JobTitle)
    {
        public SubmitTestimonialCommand ToCommand(
            Guid? workspaceId, string? workspaceSlug, string formSlug, string clientIp) => new(
            workspaceId, workspaceSlug, formSlug, TurnstileToken,
            Content ?? string.Empty, Rating, Name ?? string.Empty,
            Email, Company, JobTitle, clientIp);
    }
}
