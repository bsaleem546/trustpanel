using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;

namespace TrustPanel.Api.Middleware;

/// <summary>
/// Populates the scoped <see cref="WorkspaceContext"/> for the request from, in order:
/// authenticated workspace claim, route value, or verified custom domain host
/// (public form/widget requests only).
/// </summary>
public sealed class WorkspaceResolutionMiddleware
{
    private static readonly string[] HostResolvedPathPrefixes = ["/api/public", "/collect"];

    private readonly RequestDelegate _next;

    public WorkspaceResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, WorkspaceContext workspaceContext)
    {
        workspaceContext.WorkspaceId =
            FromClaims(context)
            ?? FromRoute(context)
            ?? await FromCustomDomainAsync(context);

        await _next(context);
    }

    private static Guid? FromClaims(HttpContext context)
    {
        var value = context.User.FindFirst(AppClaims.WorkspaceId)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static Guid? FromRoute(HttpContext context)
    {
        var value = context.GetRouteValue("workspaceId")?.ToString();
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static async Task<Guid?> FromCustomDomainAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (!HostResolvedPathPrefixes.Any(prefix => path.StartsWithSegments(prefix)))
        {
            return null;
        }

        var db = context.RequestServices.GetService<IAppDbContext>();
        if (db is null)
        {
            return null;
        }

        var host = context.Request.Host.Host;
        return await db.Workspaces
            .Where(w => w.CustomDomain == host && w.DomainVerifiedAt != null)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(context.RequestAborted);
    }
}
