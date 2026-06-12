using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrustPanel.Domain.Users;
using TrustPanel.Infrastructure.Persistence;

namespace TrustPanel.IntegrationTests;

public static class TestHelpers
{
    public sealed record TestUser(Guid UserId, Guid WorkspaceId, string AccessToken, string Email);

    /// <summary>Registers, confirms, and signs in a user; sets the bearer header on the client.</summary>
    public static async Task<TestUser> CreateUserAsync(
        this PostgresApiFactory factory, HttpClient client, string email, string password = "Password123!")
    {
        var register = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password, workspaceName = $"{email} workspace" });
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var registered = await register.ReadDataAsync();
        var userId = registered.GetProperty("userId").GetGuid();
        var workspaceId = registered.GetProperty("workspaceId").GetGuid();

        var token = factory.AuthEmails.ConfirmationEmails.Single(e => e.Email == email).Token;
        var confirm = await client.PostAsJsonAsync("/api/auth/confirm-email", new { userId, token });
        confirm.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var accessToken = (await login.ReadDataAsync()).GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return new TestUser(userId, workspaceId, accessToken, email);
    }

    /// <summary>Pins the user's effective plan via a SuperAdminOverride row.</summary>
    public static async Task SetPlanAsync(this PostgresApiFactory factory, Guid userId, string planCode)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var planId = await db.Plans.Where(p => p.Code == planCode).Select(p => p.Id).SingleAsync();
        db.SuperAdminOverrides.Add(new SuperAdminOverride
        {
            UserId = userId,
            PlanId = planId,
            Reason = "test",
            CreatedByUserId = userId,
            // Later overrides win (PlanResolver orders by CreatedAt descending).
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public static async Task<T> InDbAsync<T>(
        this PostgresApiFactory factory, Func<AppDbContext, Task<T>> query)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await query(db);
    }

    public static async Task InDbAsync(
        this PostgresApiFactory factory, Func<AppDbContext, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(db);
    }

    public static async Task<JsonElement> ReadDataAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Clone();
    }

    public static async Task<JsonElement> ReadEnvelopeAsync(this HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
