using MediatR;
using System.Security.Claims;
using System.Text;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Analytics;

namespace TrustPanel.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics").RequireAuthorization();

        group.MapGet("/dashboard", async (
            Guid workspaceId, int? daysBack, ClaimsPrincipal user, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetAnalyticsDashboardQuery(
                user.GetUserId(), workspaceId, daysBack ?? 30));
            return ApiResults.Ok(result, "Analytics dashboard.");
        });

        group.MapGet("/export/csv", async (
            Guid workspaceId, int? daysBack, ClaimsPrincipal user,
            IMediator mediator, HttpContext httpContext) =>
        {
            var result = await mediator.Send(new GetAnalyticsDashboardQuery(
                user.GetUserId(), workspaceId, daysBack ?? 30));

            var csv = new StringBuilder();
            csv.AppendLine("Date,Submissions,Impressions");
            var dates = result.SubmissionsOverTime
                .Select(s => s.Date)
                .Union(result.ImpressionsOverTime.Select(i => i.Date))
                .Distinct()
                .OrderBy(d => d);

            foreach (var date in dates)
            {
                var subs = result.SubmissionsOverTime.FirstOrDefault(s => s.Date == date)?.Count ?? 0;
                var imps = result.ImpressionsOverTime.FirstOrDefault(i => i.Date == date)?.Count ?? 0;
                csv.AppendLine($"{date:yyyy-MM-dd},{subs},{imps}");
            }

            httpContext.Response.ContentType = "text/csv";
            httpContext.Response.Headers["Content-Disposition"] = $"attachment; filename=analytics-{workspaceId}.csv";
            await httpContext.Response.WriteAsync(csv.ToString());
            return Results.Empty;
        });
    }
}
