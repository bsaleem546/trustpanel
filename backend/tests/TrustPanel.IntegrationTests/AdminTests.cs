using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TrustPanel.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace TrustPanel.IntegrationTests;

public sealed class AdminTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public AdminTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Non_admin_gets_403_on_admin_endpoint()
    {
        var client = _factory.CreateHttpsClient();
        await _factory.CreateUserAsync(client, "admin-normal@example.com");

        var res = await client.GetAsync("/api/admin/workspaces");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Super_admin_can_list_workspaces()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "admin-super@example.com");

        // Elevate to super admin.
        await MakeSuperAdminAsync(user.UserId);

        // Re-login to get a JWT with the SuperAdmin role.
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin-super@example.com",
            password = "Password123!"
        });
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginData = await loginRes.ReadDataAsync();
        var token = loginData.GetProperty("accessToken").GetString()!;

        var superClient = _factory.CreateHttpsClient();
        superClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var res = await superClient.GetAsync("/api/admin/workspaces");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Plan_override_is_created_and_deleted()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "admin-override@example.com");
        var targetUser = await _factory.CreateUserAsync(_factory.CreateHttpsClient(), "override-target@example.com");

        await MakeSuperAdminAsync(user.UserId);

        // Re-login.
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin-override@example.com",
            password = "Password123!"
        });
        var loginData = await loginRes.ReadDataAsync();
        var token = loginData.GetProperty("accessToken").GetString()!;

        var superClient = _factory.CreateHttpsClient();
        superClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Get a plan ID.
        var planId = await _factory.InDbAsync(async db =>
        {
            var plan = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(db.Plans);
            return plan!.Id;
        });

        var createRes = await superClient.PostAsJsonAsync("/api/admin/overrides", new
        {
            targetUserId = targetUser.UserId,
            planId,
            reason = "Comp",
            expiresAt = (DateTimeOffset?)null
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var overrideData = await createRes.ReadDataAsync();
        var overrideId = overrideData.GetProperty("id").GetString()!;

        // Delete it.
        var deleteRes = await superClient.DeleteAsync($"/api/admin/overrides/{overrideId}");
        deleteRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // List — should be empty.
        var listRes = await superClient.GetAsync("/api/admin/overrides");
        var list = await listRes.ReadDataAsync();
        list.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Impersonation_returns_jwt()
    {
        var client = _factory.CreateHttpsClient();
        var admin = await _factory.CreateUserAsync(client, "admin-impersonate@example.com");
        var target = await _factory.CreateUserAsync(_factory.CreateHttpsClient(), "impersonate-target@example.com");

        await MakeSuperAdminAsync(admin.UserId);

        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin-impersonate@example.com",
            password = "Password123!"
        });
        var loginData = await loginRes.ReadDataAsync();
        var token = loginData.GetProperty("accessToken").GetString()!;

        var superClient = _factory.CreateHttpsClient();
        superClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var res = await superClient.PostAsJsonAsync("/api/admin/impersonate", new
        {
            targetUserId = target.UserId
        });
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        data.TryGetProperty("token", out _).Should().BeTrue();
    }

    private async Task MakeSuperAdminAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId.ToString());
        user!.Role = TrustPanel.Domain.Users.UserRole.SuperAdmin;
        await userManager.UpdateAsync(user);
    }
}
