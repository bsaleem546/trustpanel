using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Application.Widgets;

public sealed record GetPublicWidgetQuery(Guid WidgetId) : IRequest<PublicWidgetPayload>;

public sealed class GetPublicWidgetQueryHandler
    : IRequestHandler<GetPublicWidgetQuery, PublicWidgetPayload>
{
    private readonly IAppDbContext _db;

    public GetPublicWidgetQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PublicWidgetPayload> Handle(
        GetPublicWidgetQuery request, CancellationToken cancellationToken)
    {
        var widget = await _db.Widgets
            .FirstOrDefaultAsync(w => w.Id == request.WidgetId, cancellationToken)
            ?? throw new NotFoundException("Widget not found.");

        var query = _db.Testimonials
            .Where(t => t.WorkspaceId == widget.WorkspaceId
                     && t.Status == TestimonialStatus.Approved);

        if (widget.SelectedTestimonialIds.Count > 0)
        {
            query = query.Where(t => widget.SelectedTestimonialIds.Contains(t.Id));
        }
        else
        {
            if (widget.FilterTags.Count > 0)
                query = query.Where(t => t.Tags.Any(tag => widget.FilterTags.Contains(tag)));

            if (widget.MinimumRating.HasValue)
                query = query.Where(t => t.Rating >= widget.MinimumRating.Value);

            if (widget.FeaturedOnly)
                query = query.Where(t => t.FeaturedAt != null);

            if (widget.SourceFilter.HasValue)
                query = query.Where(t => t.Source == widget.SourceFilter.Value);
        }

        var testimonials = await query
            .OrderByDescending(t => t.FeaturedAt ?? t.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var payloads = testimonials.Select(t => new PublicTestimonialPayload(
            t.Id, t.Content, t.VideoPath, t.ThumbnailPath, t.Rating,
            t.Submitter.Name, t.Submitter.Company, t.Submitter.JobTitle,
            t.Submitter.AvatarPath, t.FeaturedAt, t.CreatedAt)).ToList();

        return new PublicWidgetPayload(
            widget.Id, widget.Type, widget.Name,
            widget.Settings, widget.CustomCss, payloads);
    }
}
