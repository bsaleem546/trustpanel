using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Analytics;
using TrustPanel.Domain.Widgets;

namespace TrustPanel.Infrastructure.Jobs;

/// <summary>
/// Nightly Hangfire job that aggregates WidgetEvent rows into WidgetAnalyticsDaily.
/// Processes the previous day to avoid incomplete-day data.
/// </summary>
public sealed class AggregateWidgetAnalyticsJob
{
    private readonly IAppDbContext _db;
    private readonly ILogger<AggregateWidgetAnalyticsJob> _logger;

    public AggregateWidgetAnalyticsJob(IAppDbContext db, ILogger<AggregateWidgetAnalyticsJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        var events = await _db.WidgetEvents
            .Where(e => DateOnly.FromDateTime(e.OccurredAt.DateTime) == yesterday)
            .ToListAsync(cancellationToken);

        var grouped = events
            .GroupBy(e => e.WidgetId)
            .ToList();

        foreach (var group in grouped)
        {
            var widgetId = group.Key;
            var workspaceId = group.First().WorkspaceId;
            var views = group.Count(e => e.Event == WidgetEventType.View);
            var clicks = group.Count(e => e.Event == WidgetEventType.Click);
            var topCountry = group
                .Where(e => e.Country != null)
                .GroupBy(e => e.Country!)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
            var topDevice = group
                .Where(e => e.Device != null)
                .GroupBy(e => e.Device!)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            var existing = await _db.WidgetAnalyticsDailies
                .FirstOrDefaultAsync(d => d.WidgetId == widgetId && d.Date == yesterday, cancellationToken);

            if (existing is not null)
            {
                existing.Views += views;
                existing.Clicks += clicks;
                existing.TopCountry = topCountry;
                existing.TopDevice = topDevice;
            }
            else
            {
                _db.WidgetAnalyticsDailies.Add(new WidgetAnalyticsDaily
                {
                    WidgetId = widgetId,
                    WorkspaceId = workspaceId,
                    Date = yesterday,
                    Views = views,
                    Clicks = clicks,
                    TopCountry = topCountry,
                    TopDevice = topDevice
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("AggregateWidgetAnalyticsJob: aggregated {Count} widgets for {Date}",
            grouped.Count, yesterday);
    }
}
