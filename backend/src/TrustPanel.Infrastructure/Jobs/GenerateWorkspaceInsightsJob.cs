using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TrustPanel.Application.Ai;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Infrastructure.Jobs;

/// <summary>
/// Generates an AI insights report for a workspace and caches it for 24 hours.
/// Cache key: insights:{workspaceId}
/// </summary>
public sealed class GenerateWorkspaceInsightsJob : IInsightsJobRunner
{
    public static string CacheKey(Guid workspaceId) => $"insights:{workspaceId}";
    public static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly IAppDbContext _db;
    private readonly IAiService _ai;
    private readonly ICacheService _cache;
    private readonly ILogger<GenerateWorkspaceInsightsJob> _logger;

    public GenerateWorkspaceInsightsJob(
        IAppDbContext db, IAiService ai, ICacheService cache,
        ILogger<GenerateWorkspaceInsightsJob> logger)
    {
        _db = db;
        _ai = ai;
        _cache = cache;
        _logger = logger;
    }

    public Task RunAsync(Guid workspaceId) => RunAsync(workspaceId, CancellationToken.None);

    public async Task RunAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var workspace = await _db.Workspaces
            .Where(w => w.Id == workspaceId)
            .Select(w => new { w.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (workspace is null) return;

        var recentContent = await _db.Testimonials
            .Where(t => t.WorkspaceId == workspaceId
                     && t.Status == TestimonialStatus.Approved
                     && t.Content != null)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => t.Content!)
            .ToListAsync(cancellationToken);

        var report = await _ai.GenerateInsightsAsync(recentContent, workspace.Name, cancellationToken);
        if (report is null)
        {
            _logger.LogInformation("Insights generation skipped for workspace {WorkspaceId}: AI unavailable.", workspaceId);
            return;
        }

        var json = JsonSerializer.Serialize(report);
        await _cache.SetAsync(CacheKey(workspaceId), json, CacheTtl, cancellationToken);
        _logger.LogInformation("Insights cached for workspace {WorkspaceId}.", workspaceId);
    }
}
