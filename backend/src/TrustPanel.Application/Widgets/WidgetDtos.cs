using TrustPanel.Domain.Testimonials;
using TrustPanel.Domain.Widgets;

namespace TrustPanel.Application.Widgets;

public sealed record WidgetDto(
    Guid Id,
    Guid WorkspaceId,
    WidgetType Type,
    string Name,
    IReadOnlyList<string> FilterTags,
    int? MinimumRating,
    bool FeaturedOnly,
    IReadOnlyList<Guid> SelectedTestimonialIds,
    TestimonialSource? SourceFilter,
    WidgetSettings Settings,
    string? CustomCss,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static WidgetDto From(Widget w) => new(
        w.Id, w.WorkspaceId, w.Type, w.Name, w.FilterTags,
        w.MinimumRating, w.FeaturedOnly, w.SelectedTestimonialIds,
        w.SourceFilter, w.Settings, w.CustomCss, w.CreatedAt, w.UpdatedAt);
}

public sealed record PublicTestimonialPayload(
    Guid Id,
    string Content,
    string? VideoPath,
    string? ThumbnailPath,
    int? Rating,
    string SubmitterName,
    string? SubmitterCompany,
    string? SubmitterJobTitle,
    string? SubmitterAvatarPath,
    DateTimeOffset? FeaturedAt,
    DateTimeOffset CreatedAt);

public sealed record PublicWidgetPayload(
    Guid Id,
    WidgetType Type,
    string Name,
    WidgetSettings Settings,
    string? CustomCss,
    IReadOnlyList<PublicTestimonialPayload> Testimonials);
