using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrustPanel.Application.Ai;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Infrastructure.Jobs;

/// <summary>
/// Scores sentiment and extracts a highlight for a testimonial, then applies the
/// auto-approve rule: rating &gt;= 4 and sentiment &gt; 0.4 promotes Pending to Approved.
/// </summary>
public sealed class AnalyzeTestimonialSentimentJob
{
    public const int AutoApproveMinimumRating = 4;
    public const double AutoApproveMinimumSentiment = 0.4;

    private readonly IAppDbContext _db;
    private readonly IAiService _ai;
    private readonly ILogger<AnalyzeTestimonialSentimentJob> _logger;

    public AnalyzeTestimonialSentimentJob(
        IAppDbContext db, IAiService ai, ILogger<AnalyzeTestimonialSentimentJob> logger)
    {
        _db = db;
        _ai = ai;
        _logger = logger;
    }

    public async Task RunAsync(Guid testimonialId, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == testimonialId, cancellationToken);
        if (testimonial is null || string.IsNullOrWhiteSpace(testimonial.Content))
        {
            return;
        }

        var result = await _ai.AnalyzeSentimentAsync(testimonial.Content, cancellationToken);
        if (result is null)
        {
            _logger.LogInformation(
                "Sentiment analysis skipped for testimonial {TestimonialId}: AI unavailable.",
                testimonialId);
            return;
        }

        testimonial.SentimentScore = result.Score;
        testimonial.Highlight = result.Highlight ?? testimonial.Highlight;

        if (testimonial.Status == TestimonialStatus.Pending
            && testimonial.Rating >= AutoApproveMinimumRating
            && result.Score > AutoApproveMinimumSentiment)
        {
            testimonial.Status = TestimonialStatus.Approved;
        }

        testimonial.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
