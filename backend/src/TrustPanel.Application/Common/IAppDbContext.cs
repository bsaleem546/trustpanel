using Microsoft.EntityFrameworkCore;
using TrustPanel.Domain.Analytics;
using TrustPanel.Domain.Billing;
using TrustPanel.Domain.Common;
using TrustPanel.Domain.Email;
using TrustPanel.Domain.Forms;
using TrustPanel.Domain.Integrations;
using TrustPanel.Domain.Teams;
using TrustPanel.Domain.Testimonials;
using TrustPanel.Domain.Users;
using TrustPanel.Domain.Widgets;
using TrustPanel.Domain.Workspaces;

namespace TrustPanel.Application.Common;

public interface IAppDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Plan> Plans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<SuperAdminOverride> SuperAdminOverrides { get; }
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<CollectionForm> CollectionForms { get; }
    DbSet<Testimonial> Testimonials { get; }
    DbSet<Widget> Widgets { get; }
    DbSet<WidgetEvent> WidgetEvents { get; }
    DbSet<WidgetAnalyticsDaily> WidgetAnalyticsDailies { get; }
    DbSet<EmailLog> EmailLogs { get; }
    DbSet<EmailSuppression> EmailSuppressions { get; }
    DbSet<ApiKey> ApiKeys { get; }
    DbSet<WebhookEndpoint> WebhookEndpoints { get; }
    DbSet<ImportSource> ImportSources { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
