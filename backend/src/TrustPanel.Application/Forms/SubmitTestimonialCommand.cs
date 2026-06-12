using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Billing;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Forms;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Application.Forms;

/// <summary>Operational knobs for public submissions, bound from environment configuration.</summary>
public sealed record SubmissionOptions(int RateLimitPerHour);

public sealed record SubmitTestimonialResult(
    Guid TestimonialId, ThankYouConfigDto ThankYou, RewardConfigDto? Reward);

public sealed record SubmitTestimonialCommand(
    Guid? WorkspaceId,
    string? WorkspaceSlug,
    string FormSlug,
    string? TurnstileToken,
    string Content,
    int? Rating,
    string Name,
    string? Email,
    string? Company,
    string? JobTitle,
    string ClientIp) : IRequest<SubmitTestimonialResult>;

public sealed class SubmitTestimonialCommandValidator : AbstractValidator<SubmitTestimonialCommand>
{
    public SubmitTestimonialCommandValidator()
    {
        RuleFor(c => c.Content).NotEmpty().MaximumLength(5000);
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
        RuleFor(c => c.Rating).InclusiveBetween(1, 5).When(c => c.Rating.HasValue);
        RuleFor(c => c.Email).EmailAddress().When(c => !string.IsNullOrEmpty(c.Email));
        RuleFor(c => c.Company).MaximumLength(128);
        RuleFor(c => c.JobTitle).MaximumLength(128);
    }
}

public sealed class SubmitTestimonialCommandHandler
    : IRequestHandler<SubmitTestimonialCommand, SubmitTestimonialResult>
{
    private readonly IAppDbContext _db;
    private readonly ITurnstileVerifier _turnstile;
    private readonly IRateLimiter _rateLimiter;
    private readonly IJobScheduler _jobs;
    private readonly IPlanResolver _planResolver;
    private readonly SubmissionOptions _options;
    private readonly ISubmissionJobDispatcher _jobDispatcher;

    public SubmitTestimonialCommandHandler(
        IAppDbContext db,
        ITurnstileVerifier turnstile,
        IRateLimiter rateLimiter,
        IJobScheduler jobs,
        IPlanResolver planResolver,
        SubmissionOptions options,
        ISubmissionJobDispatcher jobDispatcher)
    {
        _db = db;
        _turnstile = turnstile;
        _rateLimiter = rateLimiter;
        _jobs = jobs;
        _planResolver = planResolver;
        _options = options;
        _jobDispatcher = jobDispatcher;
    }

    public async Task<SubmitTestimonialResult> Handle(
        SubmitTestimonialCommand request, CancellationToken cancellationToken)
    {
        var workspace = await PublicWorkspaceResolver.ResolveAsync(
            _db, request.WorkspaceId, request.WorkspaceSlug, cancellationToken);

        var form = await _db.CollectionForms.FirstOrDefaultAsync(
            f => f.WorkspaceId == workspace.Id && f.Slug == request.FormSlug && f.IsActive,
            cancellationToken)
            ?? throw new NotFoundException("Form not found.");

        if (form.AllowedSubmissionType == SubmissionType.Video)
        {
            throw new ValidationException(
                [new ValidationFailure("content", "This form only accepts video testimonials.")]);
        }

        var allowed = await _rateLimiter.TryConsumeAsync(
            $"submit:{form.Id}:{request.ClientIp}",
            _options.RateLimitPerHour,
            TimeSpan.FromHours(1),
            cancellationToken);
        if (!allowed)
        {
            throw new RateLimitedException(
                "Too many submissions from this connection. Please try again later.");
        }

        var human = await _turnstile.VerifyAsync(
            request.TurnstileToken, request.ClientIp, cancellationToken);
        if (!human)
        {
            throw new ValidationException(
                [new ValidationFailure("turnstileToken", "Captcha verification failed. Please retry.")]);
        }

        if (form.QuestionConfig.RequireEmail && string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException(
                [new ValidationFailure("email", "Email is required for this form.")]);
        }

        var testimonial = new Testimonial
        {
            WorkspaceId = workspace.Id,
            CollectionFormId = form.Id,
            Type = TestimonialType.Text,
            Content = request.Content.Trim(),
            Rating = form.QuestionConfig.CollectRating ? request.Rating : null,
            Status = TestimonialStatus.Pending,
            Source = TestimonialSource.Form,
            Submitter = new TestimonialSubmitter
            {
                Name = request.Name.Trim(),
                Email = request.Email?.Trim(),
                Company = form.QuestionConfig.CollectCompany ? request.Company?.Trim() : null,
                JobTitle = form.QuestionConfig.CollectJobTitle ? request.JobTitle?.Trim() : null
            }
        };

        _db.Testimonials.Add(testimonial);
        await _db.SaveChangesAsync(cancellationToken);

        var ownerPlan = await _planResolver.ResolveForUserAsync(
            workspace.OwnerUserId, cancellationToken);
        _jobDispatcher.DispatchSubmissionJobs(
            _jobs, testimonial.Id, hasAi: ownerPlan.Plan.HasAiFeatures);

        return new SubmitTestimonialResult(
            testimonial.Id,
            ThankYouConfigDto.From(form.ThankYouConfig),
            form.RewardConfig.Enabled ? RewardConfigDto.From(form.RewardConfig) : null);
    }
}

/// <summary>
/// Indirection so Application stays free of Infrastructure job types: Infrastructure
/// supplies the dispatcher that knows the concrete Hangfire job classes.
/// </summary>
public interface ISubmissionJobDispatcher
{
    void DispatchSubmissionJobs(IJobScheduler scheduler, Guid testimonialId, bool hasAi);
}
