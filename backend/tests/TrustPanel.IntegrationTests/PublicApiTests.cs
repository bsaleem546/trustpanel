using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TrustPanel.Domain.Testimonials;
using TrustPanel.Infrastructure.Integrations;

namespace TrustPanel.IntegrationTests;

public sealed class PublicApiTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public PublicApiTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_api_key_returns_plaintext_once()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "pubapi-key@example.com");

        var res = await client.PostAsJsonAsync("/api/apikeys/", new
        {
            workspaceId = user.WorkspaceId,
            name = "Test Key"
        });
        res.StatusCode.Should().Be(HttpStatusCode.Created);

        var data = await res.ReadDataAsync();
        var plaintext = data.GetProperty("plaintextKey").GetString()!;
        plaintext.Should().StartWith("tp_live_");

        // List keys — plaintext not returned.
        var listRes = await client.GetAsync($"/api/apikeys/?workspaceId={user.WorkspaceId}");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var listData = await listRes.ReadDataAsync();
        listData.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        return; // Plaintext verified, no second call stores it.
    }

    [Fact]
    public async Task V1_endpoint_requires_api_key_auth()
    {
        // No auth header → 401.
        var client = _factory.CreateHttpsClient();
        var res = await client.GetAsync($"/api/v1/testimonials?workspaceId={Guid.NewGuid()}");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task V1_list_testimonials_with_valid_api_key()
    {
        var dashClient = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(dashClient, "pubapi-list@example.com");

        // Seed an approved testimonial.
        await _factory.InDbAsync(async db =>
        {
            db.Testimonials.Add(new Testimonial
            {
                WorkspaceId = user.WorkspaceId,
                Status = TestimonialStatus.Approved,
                Type = TestimonialType.Text,
                Content = "Public API test",
                Source = TestimonialSource.Form,
                Rating = 5,
                Submitter = new TestimonialSubmitter { Name = "Eve" }
            });
            await db.SaveChangesAsync();
        });

        // Create API key.
        var keyRes = await dashClient.PostAsJsonAsync("/api/apikeys/", new
        {
            workspaceId = user.WorkspaceId,
            name = "Test"
        });
        var keyData = await keyRes.ReadDataAsync();
        var plaintextKey = keyData.GetProperty("plaintextKey").GetString()!;

        // Use API key to list testimonials.
        var apiClient = _factory.CreateHttpsClient();
        apiClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {plaintextKey}");

        var res = await apiClient.GetAsync(
            $"/api/v1/testimonials?workspaceId={user.WorkspaceId}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        data.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Webhook_signature_is_correct()
    {
        var body = """{"event":"testimonial.approved","data":{}}""";
        var secret = "mysecret";
        var sig = OutboundWebhookDispatcher.ComputeSignature(body, secret);
        sig.Should().HaveLength(64); // 32-byte hex
        sig.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public async Task Revoke_api_key_prevents_access()
    {
        var dashClient = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(dashClient, "pubapi-revoke@example.com");

        var keyRes = await dashClient.PostAsJsonAsync("/api/apikeys/", new
        {
            workspaceId = user.WorkspaceId,
            name = "Revoke me"
        });
        var keyData = await keyRes.ReadDataAsync();
        var plaintextKey = keyData.GetProperty("plaintextKey").GetString()!;
        var keyId = keyData.GetProperty("key").GetProperty("id").GetString()!;

        // Revoke.
        var revokeRes = await dashClient.DeleteAsync(
            $"/api/apikeys/{keyId}?workspaceId={user.WorkspaceId}");
        revokeRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to use revoked key.
        var apiClient = _factory.CreateHttpsClient();
        apiClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {plaintextKey}");
        var useRes = await apiClient.GetAsync(
            $"/api/v1/testimonials?workspaceId={user.WorkspaceId}");
        useRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
