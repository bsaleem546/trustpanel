using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Analytics;

public class WidgetAnalyticsDaily : Entity
{
    public Guid WidgetId { get; set; }
    public Guid WorkspaceId { get; set; }
    public DateOnly Date { get; set; }
    public long Views { get; set; }
    public long Clicks { get; set; }
    public long UniqueVisitors { get; set; }
    public string? TopCountry { get; set; }
    public string? TopDevice { get; set; }
}
