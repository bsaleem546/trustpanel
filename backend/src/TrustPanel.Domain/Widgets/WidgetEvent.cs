using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Widgets;

public class WidgetEvent : Entity
{
    public Guid WidgetId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid? TestimonialId { get; set; }
    public WidgetEventType Event { get; set; }
    public string? Country { get; set; }
    public string? Device { get; set; }
    public string? Referrer { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
