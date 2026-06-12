using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace TrustPanel.IntegrationTests;

public sealed class AuthFlowTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public AuthFlowTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_confirm_login_refresh_and_revoke_flow_works()
    {
        var client = _factory.CreateHttpsClient();
        const string email = "flow@example.com";
        const string password = "Sup3rSecret!";

        // Register
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password, workspaceName = "Flow Co" });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registered = await ReadData(registerResponse);
        var userId = registered.GetProperty("userId").GetGuid();
        registered.GetProperty("workspaceId").GetGuid().Should().NotBeEmpty();

        // Login before confirmation is rejected
        var earlyLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        earlyLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Confirm email with the captured token
        var confirmation = _factory.AuthEmails.ConfirmationEmails.Single(e => e.Email == email);
        confirmation.UserId.Should().Be(userId);
        var confirmResponse = await client.PostAsJsonAsync("/api/auth/confirm-email",
            new { userId, token = confirmation.Token });
        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Login
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Headers.GetValues("Set-Cookie")
            .Should().Contain(c => c.StartsWith("tp_refresh=") && c.Contains("httponly"));
        var login = await ReadData(loginResponse);
        var accessToken = login.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrEmpty();
        login.GetProperty("workspaceId").GetGuid().Should().NotBeEmpty();

        // Authenticated /me
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadData(meResponse)).GetProperty("email").GetString().Should().Be(email);

        // Session listing shows the active session
        var sessionsResponse = await client.GetAsync("/api/auth/sessions");
        sessionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var sessions = await ReadData(sessionsResponse);
        sessions.GetProperty("total").GetInt32().Should().Be(1);
        var sessionId = sessions.GetProperty("items")[0].GetProperty("sessionId").GetGuid();

        // Refresh rotates the token (cookie round-trips automatically)
        var refreshResponse = await client.PostAsync("/api/auth/refresh", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await ReadData(refreshResponse);
        refreshed.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();

        // Revoke the session, after which refresh is rejected
        var revokeResponse = await client.DeleteAsync($"/api/auth/sessions/{sessionId}");
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshAfterRevoke = await client.PostAsync("/api/auth/refresh", null);
        refreshAfterRevoke.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Password_reset_flow_revokes_sessions_and_allows_new_password()
    {
        var client = _factory.CreateHttpsClient();
        const string email = "reset@example.com";
        const string password = "OriginalPass1!";
        const string newPassword = "BrandNewPass1!";

        await RegisterAndConfirm(client, email, password);
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        // Request reset; the captured token resets the password
        var forgot = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email });
        forgot.StatusCode.Should().Be(HttpStatusCode.OK);
        var resetToken = _factory.AuthEmails.PasswordResetEmails.Single(e => e.Email == email).Token;
        var reset = await client.PostAsJsonAsync("/api/auth/reset-password",
            new { email, token = resetToken, newPassword });
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        // Old refresh token no longer works; old password rejected; new password accepted
        var refreshAfterReset = await client.PostAsync("/api/auth/refresh", null);
        refreshAfterReset.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var oldLogin = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var newLogin = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = newPassword });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_with_invalid_payload_returns_validation_envelope()
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "not-an-email", password = "short" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("status").GetBoolean().Should().BeFalse();
        var errors = document.RootElement.GetProperty("errors");
        errors.TryGetProperty("email", out _).Should().BeTrue();
        errors.TryGetProperty("password", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Duplicate_registration_returns_conflict_envelope()
    {
        var client = _factory.CreateHttpsClient();
        const string email = "duplicate@example.com";

        var first = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password123!" });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/auth/register",
            new { email, password = "Password123!" });
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Protected_endpoints_reject_anonymous_requests_with_envelope()
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.GetAsync("/api/auth/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetInt32().Should().Be(401);
        document.RootElement.GetProperty("status").GetBoolean().Should().BeFalse();
    }

    private async Task RegisterAndConfirm(HttpClient client, string email, string password)
    {
        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        register.StatusCode.Should().Be(HttpStatusCode.Created);
        var userId = (await ReadData(register)).GetProperty("userId").GetGuid();
        var token = _factory.AuthEmails.ConfirmationEmails.Single(e => e.Email == email).Token;
        var confirm = await client.PostAsJsonAsync("/api/auth/confirm-email", new { userId, token });
        confirm.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<JsonElement> ReadData(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("data").Clone();
    }
}
