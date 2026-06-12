using TrustPanel.Domain.Common;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Domain.Widgets;

public class Widget : Entity
{
    public Guid WorkspaceId { get; set; }
    public WidgetType Type { get; set; } = WidgetType.Carousel;
    public string Name { get; set; } = string.Empty;
    public List<string> FilterTags { get; set; } = [];
    public int? MinimumRating { get; set; }
    public bool FeaturedOnly { get; set; }
    public List<Guid> SelectedTestimonialIds { get; set; } = [];
    public TestimonialSource? SourceFilter { get; set; }
    public WidgetSettings Settings { get; set; } = new();
    public string? CustomCss { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
