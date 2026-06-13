using MediatR;
using System.Security.Claims;
using System.Text;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Privacy;

namespace TrustPanel.Api.Endpoints;

public static class GdprEndpoints
{
    public static void MapGdprEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gdpr").RequireAuthorization();

        group.MapGet("/export", async (
            Guid workspaceId, ClaimsPrincipal user, IMediator mediator, HttpContext httpContext) =>
        {
            var data = await mediator.Send(
                new GetGdprExportQuery(user.GetUserId(), workspaceId));

            var csv = new StringBuilder();
            csv.AppendLine("TestimonialId,SubmitterName,SubmitterEmail,SubmitterCompany,Content,Rating,CreatedAt");
            foreach (var row in data)
            {
                csv.AppendLine(string.Join(",",
                    Escape(row.TestimonialId.ToString()),
                    Escape(row.SubmitterName),
                    Escape(row.SubmitterEmail),
                    Escape(row.SubmitterCompany),
                    Escape(row.Content),
                    row.Rating?.ToString() ?? string.Empty,
                    row.CreatedAt.ToString("O")));
            }

            httpContext.Response.ContentType = "text/csv";
            httpContext.Response.Headers["Content-Disposition"] =
                $"attachment; filename=gdpr-export-{workspaceId}.csv";
            await httpContext.Response.WriteAsync(csv.ToString());
            return Results.Empty;
        });

        group.MapDelete("/delete", async (
            Guid workspaceId, string email, ClaimsPrincipal user, IMediator mediator) =>
        {
            var count = await mediator.Send(
                new GdprDeleteCommand(user.GetUserId(), workspaceId, email));
            return ApiResults.Ok(new { deleted = count }, $"Deleted personal data for {count} record(s).");
        });
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
