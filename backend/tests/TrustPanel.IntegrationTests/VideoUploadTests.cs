using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace TrustPanel.IntegrationTests;

public sealed class VideoUploadTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public VideoUploadTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Valid_video_request_returns_presigned_upload_url()
    {
        var client = _factory.CreateHttpsClient();
        await _factory.CreateUserAsync(client, "video-upload@example.com");

        var res = await client.PostAsJsonAsync("/api/uploads/video", new
        {
            contentType = "video/mp4",
            fileSizeBytes = 10 * 1024 * 1024 // 10 MB
        });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await res.ReadDataAsync();
        data.GetProperty("uploadUrl").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetProperty("objectKey").GetString().Should().StartWith("videos/");
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    public async Task Disallowed_mime_type_returns_400(string mimeType)
    {
        var client = _factory.CreateHttpsClient();
        await _factory.CreateUserAsync(client, $"video-mime-{mimeType.Replace("/", "-")}@example.com");

        var res = await client.PostAsJsonAsync("/api/uploads/video", new
        {
            contentType = mimeType,
            fileSizeBytes = 1024
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Oversized_upload_request_returns_400()
    {
        var client = _factory.CreateHttpsClient();
        await _factory.CreateUserAsync(client, "video-oversize@example.com");

        // Default max is 250 MB; send 300 MB.
        var res = await client.PostAsJsonAsync("/api/uploads/video", new
        {
            contentType = "video/mp4",
            fileSizeBytes = 300L * 1024 * 1024
        });

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Presigned_read_url_returned_for_valid_key()
    {
        var client = _factory.CreateHttpsClient();
        await _factory.CreateUserAsync(client, "video-read-url@example.com");

        var res = await client.GetAsync("/api/uploads/read-url?objectKey=videos/test-key");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await res.ReadDataAsync();
        data.GetProperty("readUrl").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
