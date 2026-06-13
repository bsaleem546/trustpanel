using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrustPanel.Application.Ai;
using TrustPanel.Application.Common;

namespace TrustPanel.Infrastructure.Jobs;

/// <summary>
/// Generates a reply suggestion for a testimonial and caches it for 24 hours.
/// Cache key: reply-suggestion:{testimonialId}
/// </summary>
public sealed class SuggestReplyJob : IReplyJobRunner
{
    public static string CacheKey(Guid testimonialId) => $"reply-suggestion:{testimonialId}";
    public static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private readonly IAppDbContext _db;
    private readonly IAiService _ai;
    private readonly ICacheService _cache;
    private readonly ILogger<SuggestReplyJob> _logger;

    public SuggestReplyJob(IAppDbContext db, IAiService ai, ICacheService cache, ILogger<SuggestReplyJob> logger)
    {
        _db = db;
        _ai = ai;
        _cache = cache;
        _logger = logger;
    }

    public Task RunAsync(Guid testimonialId, Guid workspaceId) => RunAsync(testimonialId, workspaceId, CancellationToken.None);

    public async Task RunAsync(Guid testimonialId, Guid workspaceId, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .Where(t => t.Id == testimonialId)
            .Select(t => new { t.Content })
            .FirstOrDefaultAsync(cancellationToken);

        if (testimonial is null || string.IsNullOrWhiteSpace(testimonial.Content)) return;

        var workspaceName = await _db.Workspaces
            .Where(w => w.Id == workspaceId)
            .Select(w => w.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var reply = await _ai.SuggestReplyAsync(testimonial.Content, workspaceName, cancellationToken);
        if (reply is null)
        {
            _logger.LogInformation("Reply suggestion skipped for testimonial {TestimonialId}: AI unavailable.", testimonialId);
            return;
        }

        await _cache.SetAsync(CacheKey(testimonialId), reply, CacheTtl, cancellationToken);
        _logger.LogInformation("Reply suggestion cached for testimonial {TestimonialId}.", testimonialId);
    }
}
