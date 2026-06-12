using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrustPanel.IntegrationTests;

public sealed class FoundationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FoundationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseSetting("environment", "Testing"));
    }

    [Fact]
    public async Task Readiness_endpoint_returns_standard_envelope()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .Should()
            .BeEquivalentTo("code", "status", "data", "message", "error", "errors");
        document.RootElement.GetProperty("data").GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task Responses_echo_provided_correlation_id()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-Id", "test-correlation-id");

        using var response = await client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle()
            .Which.Should().Be("test-correlation-id");
    }

    [Fact]
    public async Task Responses_generate_correlation_id_when_missing()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");

        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle()
            .Which.Should().NotBeNullOrWhiteSpace();
    }
}
