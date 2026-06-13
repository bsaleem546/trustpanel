using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Domain.Teams;

namespace TrustPanel.IntegrationTests;

public sealed class TeamTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public TeamTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Invite_and_accept_flow_adds_member()
    {
        var ownerClient = _factory.CreateHttpsClient();
        var owner = await _factory.CreateUserAsync(ownerClient, "team-owner@example.com");

        // Invite a new email.
        var inviteRes = await ownerClient.PostAsJsonAsync("/api/team/invite", new
        {
            workspaceId = owner.WorkspaceId,
            email = "team-invitee@example.com",
            role = (int)WorkspaceRole.Admin
        });
        inviteRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var inviteData = await inviteRes.ReadDataAsync();
        var token = inviteData.GetProperty("token").GetString()!;

        // Invitee registers and accepts.
        var inviteeClient = _factory.CreateHttpsClient();
        await _factory.CreateUserAsync(inviteeClient, "team-invitee@example.com");

        var acceptRes = await inviteeClient.PostAsJsonAsync("/api/team/accept", new { token });
        acceptRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Owner lists members.
        var listRes = await ownerClient.GetAsync(
            $"/api/team/?workspaceId={owner.WorkspaceId}");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var members = await listRes.ReadDataAsync();
        members.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Expired_invitation_is_rejected()
    {
        var ownerClient = _factory.CreateHttpsClient();
        var owner = await _factory.CreateUserAsync(ownerClient, "team-owner2@example.com");

        // Create an expired invitation directly in DB.
        await _factory.InDbAsync(async db =>
        {
            db.WorkspaceMembers.Add(new WorkspaceMember
            {
                WorkspaceId = owner.WorkspaceId,
                InvitedEmail = "expired@example.com",
                Role = WorkspaceRole.Viewer,
                InvitationTokenHash = TrustPanel.Application.Teams.InviteMemberCommandHandler.HashToken("expiredtoken"),
                InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(-1) // already expired
            });
            await db.SaveChangesAsync();
        });

        var inviteeClient = _factory.CreateHttpsClient();
        await _factory.CreateUserAsync(inviteeClient, "expired@example.com");

        var res = await inviteeClient.PostAsJsonAsync("/api/team/accept", new { token = "expiredtoken" });
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Remove_member_removes_from_workspace()
    {
        var ownerClient = _factory.CreateHttpsClient();
        var owner = await _factory.CreateUserAsync(ownerClient, "team-remove-owner@example.com");

        Guid memberId = default;
        await _factory.InDbAsync(async db =>
        {
            var member = new WorkspaceMember
            {
                WorkspaceId = owner.WorkspaceId,
                InvitedEmail = "toremove@example.com",
                Role = WorkspaceRole.Viewer,
                AcceptedAt = DateTimeOffset.UtcNow
            };
            db.WorkspaceMembers.Add(member);
            await db.SaveChangesAsync();
            memberId = member.Id;
        });

        var res = await ownerClient.DeleteAsync(
            $"/api/team/{memberId}?workspaceId={owner.WorkspaceId}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var count = await _factory.InDbAsync(db =>
            db.WorkspaceMembers.CountAsync(m => m.Id == memberId));
        count.Should().Be(0);
    }

    [Fact]
    public async Task Duplicate_invite_to_same_email_returns_conflict()
    {
        var ownerClient = _factory.CreateHttpsClient();
        var owner = await _factory.CreateUserAsync(ownerClient, "team-dup-owner@example.com");

        await ownerClient.PostAsJsonAsync("/api/team/invite", new
        {
            workspaceId = owner.WorkspaceId,
            email = "dup-invitee@example.com",
            role = (int)WorkspaceRole.Viewer
        });

        var secondRes = await ownerClient.PostAsJsonAsync("/api/team/invite", new
        {
            workspaceId = owner.WorkspaceId,
            email = "dup-invitee@example.com",
            role = (int)WorkspaceRole.Viewer
        });
        secondRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
