using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Application.Analytics;

public sealed record DailyCount(DateOnly Date, long Count);
public sealed record RatingBucket(int Rating, long Count);
public sealed record StringBucket(string Key, long Count);

public sealed record AnalyticsDashboardDto(
    IReadOnlyList<DailyCount> SubmissionsOverTime,
    IReadOnlyList<DailyCount> ImpressionsOverTime,
    IReadOnlyList<RatingBucket> RatingDistribution,
    IReadOnlyList<StringBucket> TopCountries,
    IReadOnlyList<StringBucket> TopDevices,
    long TotalApproved,
    long TotalPending);

public sealed record GetAnalyticsDashboardQuery(
    Guid UserId, Guid WorkspaceId, int DaysBack = 30)
    : IRequest<AnalyticsDashboardDto>;

public sealed class GetAnalyticsDashboardQueryHandler
    : IRequestHandler<GetAnalyticsDashboardQuery, AnalyticsDashboardDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public GetAnalyticsDashboardQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<AnalyticsDashboardDto> Handle(
        GetAnalyticsDashboardQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireMemberAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var since = DateTimeOffset.UtcNow.AddDays(-request.DaysBack);
        var sinceDate = DateOnly.FromDateTime(since.DateTime);

        var submissionRows = await _db.Testimonials
            .Where(t => t.WorkspaceId == request.WorkspaceId && t.CreatedAt >= since)
            .Select(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
        var submissions = submissionRows
            .GroupBy(d => DateOnly.FromDateTime(d.DateTime))
            .Select(g => new DailyCount(g.Key, g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        var ratingRows = await _db.Testimonials
            .Where(t => t.WorkspaceId == request.WorkspaceId && t.Rating != null)
            .Select(t => t.Rating!.Value)
            .ToListAsync(cancellationToken);
        var ratingDist = ratingRows
            .GroupBy(r => r)
            .Select(g => new RatingBucket(g.Key, g.Count()))
            .OrderBy(r => r.Rating)
            .ToList();

        var impressionRows = await _db.WidgetAnalyticsDailies
            .Where(d => d.WorkspaceId == request.WorkspaceId && d.Date >= sinceDate)
            .Select(d => new { d.Date, d.Views })
            .ToListAsync(cancellationToken);
        var impressions = impressionRows
            .GroupBy(d => d.Date)
            .Select(g => new DailyCount(g.Key, g.Sum(d => d.Views)))
            .OrderBy(d => d.Date)
            .ToList();

        var analyticsRows = await _db.WidgetAnalyticsDailies
            .Where(d => d.WorkspaceId == request.WorkspaceId && d.Date >= sinceDate)
            .Select(d => new { d.TopCountry, d.TopDevice, d.Views })
            .ToListAsync(cancellationToken);

        var countries = analyticsRows
            .Where(d => d.TopCountry != null)
            .GroupBy(d => d.TopCountry!)
            .Select(g => new StringBucket(g.Key, g.Sum(d => d.Views)))
            .OrderByDescending(b => b.Count)
            .Take(10)
            .ToList();

        var devices = analyticsRows
            .Where(d => d.TopDevice != null)
            .GroupBy(d => d.TopDevice!)
            .Select(g => new StringBucket(g.Key, g.Sum(d => d.Views)))
            .OrderByDescending(b => b.Count)
            .Take(5)
            .ToList();

        var totalApproved = await _db.Testimonials
            .CountAsync(t => t.WorkspaceId == request.WorkspaceId
                          && t.Status == TestimonialStatus.Approved, cancellationToken);
        var totalPending = await _db.Testimonials
            .CountAsync(t => t.WorkspaceId == request.WorkspaceId
                          && t.Status == TestimonialStatus.Pending, cancellationToken);

        return new AnalyticsDashboardDto(
            submissions, impressions, ratingDist, countries, devices,
            totalApproved, totalPending);
    }
}
