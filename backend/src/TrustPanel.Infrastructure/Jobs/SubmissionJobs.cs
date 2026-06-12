using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrustPanel.Application.Common;
using TrustPanel.Application.Forms;

namespace TrustPanel.Infrastructure.Jobs;

/// <summary>Wires public submissions to the concrete Hangfire job classes.</summary>
public sealed class SubmissionJobDispatcher : ISubmissionJobDispatcher
{
    public void DispatchSubmissionJobs(IJobScheduler scheduler, Guid testimonialId, bool hasAi)
    {
        scheduler.Enqueue<SendTestimonialThankYouJob>(
            job => job.RunAsync(testimonialId, CancellationToken.None));
        scheduler.Enqueue<NotifyWorkspaceOwnerJob>(
            job => job.RunAsync(testimonialId, CancellationToken.None));
        if (hasAi)
        {
            scheduler.Enqueue<AnalyzeTestimonialSentimentJob>(
                job => job.RunAsync(testimonialId, CancellationToken.None));
        }
    }
}

/// <summary>
/// Sends the thank-you email for a submission. Delivery is implemented by the email
/// system phase; until then the job logs the intent.
/// </summary>
public class SendTestimonialThankYouJob
{
    private readonly IAppDbContext _db;
    private readonly ILogger<SendTestimonialThankYouJob> _logger;

    public SendTestimonialThankYouJob(IAppDbContext db, ILogger<SendTestimonialThankYouJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public virtual async Task RunAsync(Guid testimonialId, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == testimonialId, cancellationToken);
        if (testimonial?.Submitter.Email is null)
        {
            return;
        }

        _logger.LogInformation(
            "Thank-you email queued for testimonial {TestimonialId} to {Email}.",
            testimonialId, testimonial.Submitter.Email);
    }
}

/// <summary>
/// Notifies the workspace owner of a new submission. Delivery is implemented by the
/// email system phase; until then the job logs the intent.
/// </summary>
public class NotifyWorkspaceOwnerJob
{
    private readonly IAppDbContext _db;
    private readonly ILogger<NotifyWorkspaceOwnerJob> _logger;

    public NotifyWorkspaceOwnerJob(IAppDbContext db, ILogger<NotifyWorkspaceOwnerJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public virtual async Task RunAsync(Guid testimonialId, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == testimonialId, cancellationToken);
        if (testimonial is null)
        {
            return;
        }

        _logger.LogInformation(
            "Owner notification queued for testimonial {TestimonialId} in workspace {WorkspaceId}.",
            testimonialId, testimonial.WorkspaceId);
    }
}
