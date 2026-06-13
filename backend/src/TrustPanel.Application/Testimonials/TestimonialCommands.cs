using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Application.Common.Behaviors;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Application.Testimonials;

internal static class WidgetCacheInvalidator
{
    public static async Task BustAsync(
        IAppDbContext db, ICacheService cache, Guid workspaceId, CancellationToken ct)
    {
        var widgetIds = await db.Widgets
            .Where(w => w.WorkspaceId == workspaceId)
            .Select(w => w.Id)
            .ToListAsync(ct);
        foreach (var id in widgetIds)
            await cache.RemoveAsync($"widget:{id}", ct);
    }
}

// ── Approve ──────────────────────────────────────────────────────────────────

public sealed record ApproveTestimonialCommand(Guid UserId, Guid TestimonialId) : IRequest<TestimonialDto>;

public sealed class ApproveTestimonialCommandHandler
    : IRequestHandler<ApproveTestimonialCommand, TestimonialDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IAuditTrail _audit;
    private readonly ICacheService _cache;

    public ApproveTestimonialCommandHandler(
        IAppDbContext db, WorkspaceAccess access, IAuditTrail audit, ICacheService cache)
    {
        _db = db; _access = access; _audit = audit; _cache = cache;
    }

    public async Task<TestimonialDto> Handle(
        ApproveTestimonialCommand request, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == request.TestimonialId, cancellationToken)
            ?? throw new NotFoundException("Testimonial not found.");
        await _access.RequireManageAsync(testimonial.WorkspaceId, request.UserId, cancellationToken);

        testimonial.Status = TestimonialStatus.Approved;
        testimonial.UpdatedAt = DateTimeOffset.UtcNow;
        _audit.Record(testimonial.WorkspaceId, request.UserId,
            "testimonial.approved", "Testimonial", testimonial.Id);
        await _db.SaveChangesAsync(cancellationToken);
        await WidgetCacheInvalidator.BustAsync(_db, _cache, testimonial.WorkspaceId, cancellationToken);
        return TestimonialDto.From(testimonial);
    }
}

// ── Reject ────────────────────────────────────────────────────────────────────

public sealed record RejectTestimonialCommand(Guid UserId, Guid TestimonialId) : IRequest<TestimonialDto>;

public sealed class RejectTestimonialCommandHandler
    : IRequestHandler<RejectTestimonialCommand, TestimonialDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IAuditTrail _audit;

    public RejectTestimonialCommandHandler(IAppDbContext db, WorkspaceAccess access, IAuditTrail audit)
    {
        _db = db; _access = access; _audit = audit;
    }

    public async Task<TestimonialDto> Handle(
        RejectTestimonialCommand request, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == request.TestimonialId, cancellationToken)
            ?? throw new NotFoundException("Testimonial not found.");
        await _access.RequireManageAsync(testimonial.WorkspaceId, request.UserId, cancellationToken);

        testimonial.Status = TestimonialStatus.Rejected;
        testimonial.UpdatedAt = DateTimeOffset.UtcNow;
        _audit.Record(testimonial.WorkspaceId, request.UserId,
            "testimonial.rejected", "Testimonial", testimonial.Id);
        await _db.SaveChangesAsync(cancellationToken);
        return TestimonialDto.From(testimonial);
    }
}

// ── Feature / Unfeature ───────────────────────────────────────────────────────

public sealed record FeatureTestimonialCommand(Guid UserId, Guid TestimonialId, bool Featured) : IRequest<TestimonialDto>;

public sealed class FeatureTestimonialCommandHandler
    : IRequestHandler<FeatureTestimonialCommand, TestimonialDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IAuditTrail _audit;

    public FeatureTestimonialCommandHandler(IAppDbContext db, WorkspaceAccess access, IAuditTrail audit)
    {
        _db = db; _access = access; _audit = audit;
    }

    public async Task<TestimonialDto> Handle(
        FeatureTestimonialCommand request, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == request.TestimonialId, cancellationToken)
            ?? throw new NotFoundException("Testimonial not found.");
        await _access.RequireManageAsync(testimonial.WorkspaceId, request.UserId, cancellationToken);

        testimonial.FeaturedAt = request.Featured ? DateTimeOffset.UtcNow : null;
        testimonial.UpdatedAt = DateTimeOffset.UtcNow;
        _audit.Record(testimonial.WorkspaceId, request.UserId,
            request.Featured ? "testimonial.featured" : "testimonial.unfeatured",
            "Testimonial", testimonial.Id);
        await _db.SaveChangesAsync(cancellationToken);
        return TestimonialDto.From(testimonial);
    }
}

// ── Tag ───────────────────────────────────────────────────────────────────────

public sealed record UpdateTestimonialTagsCommand(
    Guid UserId, Guid TestimonialId, IReadOnlyList<string> Tags) : IRequest<TestimonialDto>;

public sealed class UpdateTestimonialTagsCommandHandler
    : IRequestHandler<UpdateTestimonialTagsCommand, TestimonialDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IAuditTrail _audit;

    public UpdateTestimonialTagsCommandHandler(IAppDbContext db, WorkspaceAccess access, IAuditTrail audit)
    {
        _db = db; _access = access; _audit = audit;
    }

    public async Task<TestimonialDto> Handle(
        UpdateTestimonialTagsCommand request, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == request.TestimonialId, cancellationToken)
            ?? throw new NotFoundException("Testimonial not found.");
        await _access.RequireManageAsync(testimonial.WorkspaceId, request.UserId, cancellationToken);

        testimonial.Tags = request.Tags.Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0).Distinct().ToList();
        testimonial.UpdatedAt = DateTimeOffset.UtcNow;
        _audit.Record(testimonial.WorkspaceId, request.UserId,
            "testimonial.tags_updated", "Testimonial", testimonial.Id,
            new { tags = testimonial.Tags });
        await _db.SaveChangesAsync(cancellationToken);
        return TestimonialDto.From(testimonial);
    }
}

// ── Edit content ──────────────────────────────────────────────────────────────

public sealed record EditTestimonialCommand(
    Guid UserId, Guid TestimonialId, string Content, int? Rating) : IRequest<TestimonialDto>;

public sealed class EditTestimonialCommandHandler
    : IRequestHandler<EditTestimonialCommand, TestimonialDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IAuditTrail _audit;

    public EditTestimonialCommandHandler(IAppDbContext db, WorkspaceAccess access, IAuditTrail audit)
    {
        _db = db; _access = access; _audit = audit;
    }

    public async Task<TestimonialDto> Handle(
        EditTestimonialCommand request, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == request.TestimonialId, cancellationToken)
            ?? throw new NotFoundException("Testimonial not found.");
        await _access.RequireManageAsync(testimonial.WorkspaceId, request.UserId, cancellationToken);

        testimonial.Content = request.Content;
        if (request.Rating.HasValue)
        {
            testimonial.Rating = request.Rating;
        }
        testimonial.EditedAt = DateTimeOffset.UtcNow;
        testimonial.UpdatedAt = DateTimeOffset.UtcNow;
        _audit.Record(testimonial.WorkspaceId, request.UserId,
            "testimonial.edited", "Testimonial", testimonial.Id);
        await _db.SaveChangesAsync(cancellationToken);
        return TestimonialDto.From(testimonial);
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

public sealed record DeleteTestimonialCommand(Guid UserId, Guid TestimonialId) : IRequest;

public sealed class DeleteTestimonialCommandHandler
    : IRequestHandler<DeleteTestimonialCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IAuditTrail _audit;

    public DeleteTestimonialCommandHandler(IAppDbContext db, WorkspaceAccess access, IAuditTrail audit)
    {
        _db = db; _access = access; _audit = audit;
    }

    public async Task Handle(
        DeleteTestimonialCommand request, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == request.TestimonialId, cancellationToken)
            ?? throw new NotFoundException("Testimonial not found.");
        await _access.RequireManageAsync(testimonial.WorkspaceId, request.UserId, cancellationToken);

        _audit.Record(testimonial.WorkspaceId, request.UserId,
            "testimonial.deleted", "Testimonial", testimonial.Id);
        _db.Testimonials.Remove(testimonial);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ── Batch moderation ──────────────────────────────────────────────────────────

public enum BatchTestimonialAction { Approve, Reject, Delete, Feature, Unfeature }

public sealed record BatchTestimonialCommand(
    Guid UserId, Guid WorkspaceId,
    IReadOnlyList<Guid> TestimonialIds,
    BatchTestimonialAction Action) : IRequest<int>;

public sealed class BatchTestimonialCommandHandler
    : IRequestHandler<BatchTestimonialCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IAuditTrail _audit;

    public BatchTestimonialCommandHandler(IAppDbContext db, WorkspaceAccess access, IAuditTrail audit)
    {
        _db = db; _access = access; _audit = audit;
    }

    public async Task<int> Handle(
        BatchTestimonialCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var testimonials = await _db.Testimonials
            .Where(t => t.WorkspaceId == request.WorkspaceId
                     && request.TestimonialIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var t in testimonials)
        {
            switch (request.Action)
            {
                case BatchTestimonialAction.Approve:
                    t.Status = TestimonialStatus.Approved;
                    t.UpdatedAt = now;
                    _audit.Record(t.WorkspaceId, request.UserId, "testimonial.approved", "Testimonial", t.Id);
                    break;
                case BatchTestimonialAction.Reject:
                    t.Status = TestimonialStatus.Rejected;
                    t.UpdatedAt = now;
                    _audit.Record(t.WorkspaceId, request.UserId, "testimonial.rejected", "Testimonial", t.Id);
                    break;
                case BatchTestimonialAction.Delete:
                    _audit.Record(t.WorkspaceId, request.UserId, "testimonial.deleted", "Testimonial", t.Id);
                    _db.Testimonials.Remove(t);
                    break;
                case BatchTestimonialAction.Feature:
                    t.FeaturedAt = now;
                    t.UpdatedAt = now;
                    _audit.Record(t.WorkspaceId, request.UserId, "testimonial.featured", "Testimonial", t.Id);
                    break;
                case BatchTestimonialAction.Unfeature:
                    t.FeaturedAt = null;
                    t.UpdatedAt = now;
                    _audit.Record(t.WorkspaceId, request.UserId, "testimonial.unfeatured", "Testimonial", t.Id);
                    break;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return testimonials.Count;
    }
}
