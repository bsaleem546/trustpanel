using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Application.PublicApi;

public sealed record PublicTestimonialDto(
    Guid Id, string? Content, string? SubmitterName, string? SubmitterCompany,
    string? SubmitterJobTitle, string? SubmitterAvatar, int? Rating,
    IReadOnlyList<string> Tags, DateTimeOffset CreatedAt);

// ── List approved testimonials ────────────────────────────────────────────────

public sealed record ListV1TestimonialsQuery(
    Guid WorkspaceId,
    int? MinRating = null,
    string? Tag = null,
    int Page = 1,
    int PageSize = 20)
    : IRequest<IReadOnlyList<PublicTestimonialDto>>;

public sealed class ListV1TestimonialsQueryHandler
    : IRequestHandler<ListV1TestimonialsQuery, IReadOnlyList<PublicTestimonialDto>>
{
    private readonly IAppDbContext _db;

    public ListV1TestimonialsQueryHandler(IAppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<PublicTestimonialDto>> Handle(
        ListV1TestimonialsQuery request, CancellationToken cancellationToken)
    {
        var q = _db.Testimonials
            .Where(t => t.WorkspaceId == request.WorkspaceId
                     && t.Status == TestimonialStatus.Approved);

        if (request.MinRating.HasValue)
            q = q.Where(t => t.Rating >= request.MinRating.Value);

        if (!string.IsNullOrWhiteSpace(request.Tag))
            q = q.Where(t => t.Tags.Contains(request.Tag));

        return await q
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * Math.Clamp(request.PageSize, 1, 100))
            .Take(Math.Clamp(request.PageSize, 1, 100))
            .Select(t => new PublicTestimonialDto(
                t.Id, t.Content,
                t.Submitter == null ? null : t.Submitter.Name,
                t.Submitter == null ? null : t.Submitter.Company,
                t.Submitter == null ? null : t.Submitter.JobTitle,
                t.Submitter == null ? null : t.Submitter.AvatarUrl,
                t.Rating, t.Tags, t.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

// ── Get single testimonial ────────────────────────────────────────────────────

public sealed record GetV1TestimonialQuery(Guid WorkspaceId, Guid TestimonialId)
    : IRequest<PublicTestimonialDto>;

public sealed class GetV1TestimonialQueryHandler
    : IRequestHandler<GetV1TestimonialQuery, PublicTestimonialDto>
{
    private readonly IAppDbContext _db;

    public GetV1TestimonialQueryHandler(IAppDbContext db) { _db = db; }

    public async Task<PublicTestimonialDto> Handle(
        GetV1TestimonialQuery request, CancellationToken cancellationToken)
    {
        return await _db.Testimonials
            .Where(t => t.Id == request.TestimonialId
                     && t.WorkspaceId == request.WorkspaceId
                     && t.Status == TestimonialStatus.Approved)
            .Select(t => new PublicTestimonialDto(
                t.Id, t.Content,
                t.Submitter == null ? null : t.Submitter.Name,
                t.Submitter == null ? null : t.Submitter.Company,
                t.Submitter == null ? null : t.Submitter.JobTitle,
                t.Submitter == null ? null : t.Submitter.AvatarUrl,
                t.Rating, t.Tags, t.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Testimonial not found.");
    }
}

// ── Programmatic create ───────────────────────────────────────────────────────

public sealed record CreateV1TestimonialCommand(
    Guid WorkspaceId, string Content, string SubmitterName,
    string? SubmitterEmail = null, int? Rating = null)
    : IRequest<PublicTestimonialDto>;

public sealed class CreateV1TestimonialCommandHandler
    : IRequestHandler<CreateV1TestimonialCommand, PublicTestimonialDto>
{
    private readonly IAppDbContext _db;

    public CreateV1TestimonialCommandHandler(IAppDbContext db) { _db = db; }

    public async Task<PublicTestimonialDto> Handle(
        CreateV1TestimonialCommand request, CancellationToken cancellationToken)
    {
        var t = new Testimonial
        {
            WorkspaceId = request.WorkspaceId,
            Type = TestimonialType.Text,
            Status = TestimonialStatus.Approved,
            Source = TestimonialSource.Api,
            Content = request.Content,
            Rating = request.Rating,
            Submitter = new TestimonialSubmitter
            {
                Name = request.SubmitterName,
                Email = request.SubmitterEmail
            }
        };
        _db.Testimonials.Add(t);
        await _db.SaveChangesAsync(cancellationToken);

        return new PublicTestimonialDto(
            t.Id, t.Content, t.Submitter?.Name, t.Submitter?.Company,
            t.Submitter?.JobTitle, t.Submitter?.AvatarUrl,
            t.Rating, t.Tags, t.CreatedAt);
    }
}
