using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
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
using TrustPanel.Infrastructure.Identity;

namespace TrustPanel.Infrastructure.Persistence;

public class AppDbContext :
    IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>,
    IAppDbContext
{
    private readonly ICurrentWorkspace _currentWorkspace;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentWorkspace currentWorkspace)
        : base(options)
    {
        _currentWorkspace = currentWorkspace;
    }

    /// <summary>
    /// Tenant used by global query filters. Null means no tenant scoping
    /// (background jobs, public host resolution, super admin).
    /// </summary>
    public Guid? TenantId => _currentWorkspace.WorkspaceId;

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SuperAdminOverride> SuperAdminOverrides => Set<SuperAdminOverride>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<CollectionForm> CollectionForms => Set<CollectionForm>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<Widget> Widgets => Set<Widget>();
    public DbSet<WidgetEvent> WidgetEvents => Set<WidgetEvent>();
    public DbSet<WidgetAnalyticsDaily> WidgetAnalyticsDailies => Set<WidgetAnalyticsDaily>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<EmailSuppression> EmailSuppressions => Set<EmailSuppression>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<ImportSource> ImportSources => Set<ImportSource>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.Entity<Testimonial>()
            .HasQueryFilter(e => TenantId == null || e.WorkspaceId == TenantId);
        builder.Entity<Widget>()
            .HasQueryFilter(e => TenantId == null || e.WorkspaceId == TenantId);
        builder.Entity<CollectionForm>()
            .HasQueryFilter(e => TenantId == null || e.WorkspaceId == TenantId);
        builder.Entity<WidgetEvent>()
            .HasQueryFilter(e => TenantId == null || e.WorkspaceId == TenantId);
        builder.Entity<EmailLog>()
            .HasQueryFilter(e => TenantId == null || e.WorkspaceId == TenantId);
        builder.Entity<ApiKey>()
            .HasQueryFilter(e => TenantId == null || e.WorkspaceId == TenantId);
        builder.Entity<WebhookEndpoint>()
            .HasQueryFilter(e => TenantId == null || e.WorkspaceId == TenantId);
        builder.Entity<ImportSource>()
            .HasQueryFilter(e => TenantId == null || e.WorkspaceId == TenantId);
        builder.Entity<AuditLog>()
            .HasQueryFilter(e => TenantId == null || e.WorkspaceId == TenantId);
    }
}
