using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrustPanel.Domain.Billing;
using TrustPanel.Infrastructure.Jobs;
using TrustPanel.Infrastructure.Persistence;

namespace TrustPanel.IntegrationTests;

public sealed class WorkspaceManagementTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public WorkspaceManagementTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Workspace_crud_works_within_plan_limits()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "ws-crud@example.com");

        // Trial users run on Agency+ (unlimited workspaces), so a second create succeeds.
        var create = await client.PostAsJsonAsync("/api/workspaces", new { name = "Second" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync();
        var secondId = created.GetProperty("id").GetGuid();

        var list = await client.GetAsync("/api/workspaces");
        (await list.ReadDataAsync()).GetProperty("total").GetInt32().Should().Be(2);

        var rename = await client.PutAsJsonAsync($"/api/workspaces/{secondId}", new { name = "Renamed" });
        rename.StatusCode.Should().Be(HttpStatusCode.OK);
        (await rename.ReadDataAsync()).GetProperty("name").GetString().Should().Be("Renamed");

        var delete = await client.DeleteAsync($"/api/workspaces/{secondId}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);
        var listAfter = await client.GetAsync("/api/workspaces");
        (await listAfter.ReadDataAsync()).GetProperty("total").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Workspace_creation_is_blocked_at_the_plan_limit()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "ws-limit@example.com");
        await _factory.SetPlanAsync(user.UserId, PlanCodes.Starter);

        // Starter allows one workspace and the default one already exists.
        var create = await client.PostAsJsonAsync("/api/workspaces", new { name = "Overflow" });

        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var envelope = await create.ReadEnvelopeAsync();
        envelope.GetProperty("status").GetBoolean().Should().BeFalse();
        envelope.GetProperty("message").GetString().Should().Contain("plan");
    }

    [Fact]
    public async Task White_label_and_custom_email_sender_require_agency_plan()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "ws-brand@example.com");
        await _factory.SetPlanAsync(user.UserId, PlanCodes.Pro);

        var removeBranding = await client.PutAsJsonAsync(
            $"/api/workspaces/{user.WorkspaceId}/branding",
            new { showTrustPanelBranding = false });
        removeBranding.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var customSender = await client.PutAsJsonAsync(
            $"/api/workspaces/{user.WorkspaceId}/branding",
            new { emailFromName = "Acme", emailFromAddress = "hello@acme.com" });
        customSender.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Colors and logo are not gated.
        var colors = await client.PutAsJsonAsync(
            $"/api/workspaces/{user.WorkspaceId}/branding",
            new { primaryColor = "#112233" });
        colors.StatusCode.Should().Be(HttpStatusCode.OK);

        await _factory.SetPlanAsync(user.UserId, PlanCodes.Agency);
        var allowed = await client.PutAsJsonAsync(
            $"/api/workspaces/{user.WorkspaceId}/branding",
            new { showTrustPanelBranding = false, emailFromName = "Acme", emailFromAddress = "hello@acme.com" });
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
        var branding = (await allowed.ReadDataAsync()).GetProperty("branding");
        branding.GetProperty("showTrustPanelBranding").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Custom_domains_require_agency_plan()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "ws-domain-gate@example.com");
        await _factory.SetPlanAsync(user.UserId, PlanCodes.Pro);

        var response = await client.PutAsJsonAsync(
            $"/api/workspaces/{user.WorkspaceId}/domain",
            new { domain = "reviews.gated.com" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dns_verification_transitions_in_both_directions()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "ws-dns@example.com");
        const string domain = "reviews.dns-test.com";

        var save = await client.PutAsJsonAsync(
            $"/api/workspaces/{user.WorkspaceId}/domain", new { domain });
        save.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await save.ReadDataAsync();
        saved.GetProperty("verified").GetBoolean().Should().BeFalse();
        var cnameTarget = saved.GetProperty("cnameTarget").GetString()!;

        // DNS not pointed yet: stays unverified.
        var verify1 = await client.PostAsync($"/api/workspaces/{user.WorkspaceId}/domain/verify", null);
        (await verify1.ReadDataAsync()).GetProperty("verified").GetBoolean().Should().BeFalse();

        // Point the CNAME at us: verification succeeds and persists.
        _factory.Dns.SetCname(domain, cnameTarget);
        var verify2 = await client.PostAsync($"/api/workspaces/{user.WorkspaceId}/domain/verify", null);
        var verified = await verify2.ReadDataAsync();
        verified.GetProperty("verified").GetBoolean().Should().BeTrue();
        verified.GetProperty("verifiedAt").ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Null);

        // CNAME removed: verification is revoked.
        _factory.Dns.Clear(domain);
        var verify3 = await client.PostAsync($"/api/workspaces/{user.WorkspaceId}/domain/verify", null);
        (await verify3.ReadDataAsync()).GetProperty("verified").GetBoolean().Should().BeFalse();
        var verifiedAt = await _factory.InDbAsync(db => db.Workspaces
            .Where(w => w.Id == user.WorkspaceId)
            .Select(w => w.DomainVerifiedAt)
            .SingleAsync());
        verifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task Recurring_job_verifies_pending_domains()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "ws-job@example.com");
        const string domain = "reviews.job-test.com";

        var save = await client.PutAsJsonAsync(
            $"/api/workspaces/{user.WorkspaceId}/domain", new { domain });
        var cnameTarget = (await save.ReadDataAsync()).GetProperty("cnameTarget").GetString()!;
        _factory.Dns.SetCname(domain, cnameTarget);

        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<VerifyWorkspaceDomainJob>()
                .RunAsync(CancellationToken.None);
        }

        var verifiedAt = await _factory.InDbAsync(db => db.Workspaces
            .Where(w => w.Id == user.WorkspaceId)
            .Select(w => w.DomainVerifiedAt)
            .SingleAsync());
        verifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Public_requests_resolve_workspace_from_verified_custom_domain_host()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "ws-host@example.com");
        const string domain = "reviews.host-test.com";

        await _factory.InDbAsync(async db =>
        {
            var workspace = await db.Workspaces.SingleAsync(w => w.Id == user.WorkspaceId);
            workspace.CustomDomain = domain;
            workspace.DomainVerifiedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        });

        var anonymous = _factory.CreateHttpsClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/api/public/_diagnostics/workspace");
        request.Headers.Host = domain;
        var response = await anonymous.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadDataAsync()).GetProperty("workspaceId").GetGuid()
            .Should().Be(user.WorkspaceId);

        // Unknown hosts resolve to no workspace.
        using var unknown = new HttpRequestMessage(
            HttpMethod.Get, "/api/public/_diagnostics/workspace");
        unknown.Headers.Host = "unknown.example.com";
        var unknownResponse = await anonymous.SendAsync(unknown);
        (await unknownResponse.ReadDataAsync()).GetProperty("workspaceId").ValueKind
            .Should().Be(System.Text.Json.JsonValueKind.Null);
    }
}
