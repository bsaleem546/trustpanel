using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrustPanel.IntegrationTests;

public sealed class ApiResponseEnvelopeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiResponseEnvelopeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.UseSetting("environment", "Testing"));
    }

    [Fact]
    public async Task Successful_endpoint_returns_standard_response_envelope()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertEnvelope(document.RootElement);
        document.RootElement.GetProperty("code").GetInt32().Should().Be(200);
        document.RootElement.GetProperty("status").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);
        document.RootElement.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("error").GetString().Should().BeEmpty();
        document.RootElement.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task Unhandled_exception_returns_standard_response_envelope()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/_diagnostics/boom");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        AssertEnvelope(document.RootElement);
        document.RootElement.GetProperty("code").GetInt32().Should().Be(500);
        document.RootElement.GetProperty("status").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);
        document.RootElement.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("errors").ValueKind.Should().Be(JsonValueKind.Object);
    }

    private static void AssertEnvelope(JsonElement root)
    {
        root.EnumerateObject()
            .Select(property => property.Name)
            .Should()
            .BeEquivalentTo("code", "status", "data", "message", "error", "errors");
    }
}
