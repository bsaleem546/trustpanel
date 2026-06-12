using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Billing;
using TrustPanel.Domain.Testimonials;
using TrustPanel.Domain.Widgets;
using TrustPanel.Domain.Workspaces;
using TrustPanel.Infrastructure.Persistence;

namespace TrustPanel.IntegrationTests;

public sealed class TenantIsolationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    private Guid _workspaceA;
    private Guid _workspaceB;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Applying migrations against real PostgreSQL also verifies schema creation.
        await using var context = CreateContext(tenantId: null);
        await context.Database.MigrateAsync();

        var workspaceA = new Workspace { OwnerUserId = Guid.NewGuid(), Slug = "acme", Name = "Acme" };
        var workspaceB = new Workspace { OwnerUserId = Guid.NewGuid(), Slug = "globex", Name = "Globex" };
        _workspaceA = workspaceA.Id;
        _workspaceB = workspaceB.Id;

        context.Workspaces.AddRange(workspaceA, workspaceB);
        context.Testimonials.AddRange(
            new Testimonial { WorkspaceId = _workspaceA, Content = "Great product", Rating = 5 },
            new Testimonial { WorkspaceId = _workspaceA, Content = "Solid tool", Rating = 4 },
            new Testimonial { WorkspaceId = _workspaceB, Content = "Other tenant data", Rating = 5 });
        context.Widgets.AddRange(
            new Widget { WorkspaceId = _workspaceA, Name = "Acme carousel" },
            new Widget { WorkspaceId = _workspaceB, Name = "Globex grid" });
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private AppDbContext CreateContext(Guid? tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        return new AppDbContext(options, new WorkspaceContext { WorkspaceId = tenantId });
    }

    [Fact]
    public async Task Migrations_seed_the_four_plans()
    {
        await using var context = CreateContext(tenantId: null);

        var codes = await context.Plans.Select(p => p.Code).ToListAsync();

        codes.Should().BeEquivalentTo(
            PlanCodes.Starter, PlanCodes.Pro, PlanCodes.Agency, PlanCodes.AgencyPlus);
    }

    [Fact]
    public async Task Tenant_scoped_queries_only_return_current_workspace_data()
    {
        await using var context = CreateContext(_workspaceA);

        var testimonials = await context.Testimonials.ToListAsync();
        var widgets = await context.Widgets.ToListAsync();

        testimonials.Should().HaveCount(2);
        testimonials.Should().OnlyContain(t => t.WorkspaceId == _workspaceA);
        widgets.Should().ContainSingle().Which.WorkspaceId.Should().Be(_workspaceA);
    }

    [Fact]
    public async Task Tenant_scoped_queries_cannot_load_other_tenant_rows_by_id()
    {
        Guid otherTenantTestimonialId;
        await using (var unscoped = CreateContext(tenantId: null))
        {
            otherTenantTestimonialId = await unscoped.Testimonials
                .Where(t => t.WorkspaceId == _workspaceB)
                .Select(t => t.Id)
                .FirstAsync();
        }

        await using var context = CreateContext(_workspaceA);

        var leaked = await context.Testimonials
            .FirstOrDefaultAsync(t => t.Id == otherTenantTestimonialId);

        leaked.Should().BeNull();
    }

    [Fact]
    public async Task Null_tenant_context_sees_all_workspaces_for_background_jobs()
    {
        await using var context = CreateContext(tenantId: null);

        var testimonials = await context.Testimonials.ToListAsync();

        testimonials.Should().HaveCount(3);
    }
}
