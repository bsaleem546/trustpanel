using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrustPanel.Application.Email;
using TrustPanel.Domain.Email;

namespace TrustPanel.IntegrationTests;

public sealed class EmailSystemTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public EmailSystemTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Template_renderer_merges_fields_correctly()
    {
        using var scope = _factory.Services.CreateScope();
        var renderer = scope.ServiceProvider.GetRequiredService<IEmailTemplateRenderer>();

        var (subject, html) = await renderer.RenderAsync(
            EmailTemplateType.ThankYou,
            new Dictionary<string, string>
            {
                ["SubmitterName"] = "Alice",
                ["WorkspaceName"] = "ACME"
            },
            CancellationToken.None);

        subject.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("Alice");
        html.Should().Contain("ACME");
    }

    [Fact]
    public async Task Suppression_blocks_email_send()
    {
        var user = await _factory.CreateUserAsync(_factory.CreateHttpsClient(), "suppression@example.com");

        await _factory.InDbAsync(async db =>
        {
            db.EmailSuppressions.Add(new EmailSuppression
            {
                WorkspaceId = user.WorkspaceId,
                Email = "blocked@test.com",
                Reason = SuppressionReason.Bounced
            });
            await db.SaveChangesAsync();
        });

        using var scope = _factory.Services.CreateScope();
        var orchestration = scope.ServiceProvider.GetRequiredService<EmailOrchestrationService>();

        // Should complete without throwing (suppression silently blocks send).
        var act = async () => await orchestration.SendAsync(
            user.WorkspaceId, "blocked@test.com",
            EmailTemplateType.ThankYou,
            new Dictionary<string, string> { ["SubmitterName"] = "Test", ["WorkspaceName"] = "Test" });

        await act.Should().NotThrowAsync();

        var logCount = await _factory.InDbAsync(db =>
            db.EmailLogs.CountAsync(l => l.Recipient == "blocked@test.com"));
        logCount.Should().Be(0);
    }

    [Fact]
    public async Task Unsubscribe_endpoint_adds_suppression()
    {
        var client = _factory.CreateHttpsClient();

        using var scope = _factory.Services.CreateScope();
        var unsubscribe = scope.ServiceProvider.GetRequiredService<UnsubscribeService>();
        var token = unsubscribe.CreateToken("unsub@example.com");

        var res = await client.GetAsync($"/api/email/unsubscribe?token={Uri.EscapeDataString(token)}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var suppressed = await _factory.InDbAsync(db =>
            db.EmailSuppressions.AnyAsync(s => s.Email == "unsub@example.com"));
        suppressed.Should().BeTrue();
    }

    [Fact]
    public async Task Resend_webhook_updates_email_log_status()
    {
        var user = await _factory.CreateUserAsync(_factory.CreateHttpsClient(), "webhook-email@example.com");
        var messageId = "msg_resend_test_123";

        await _factory.InDbAsync(async db =>
        {
            db.EmailLogs.Add(new EmailLog
            {
                WorkspaceId = user.WorkspaceId,
                Template = EmailTemplateType.ThankYou,
                Recipient = "test@example.com",
                ProviderMessageId = messageId,
                Status = EmailStatus.Sent,
                SentAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        });

        var webhookPayload = JsonSerializer.Serialize(new
        {
            type = "email.delivered",
            data = new { email_id = messageId }
        });

        var httpClient = _factory.CreateHttpsClient();
        var res = await httpClient.PostAsync("/webhooks/resend",
            new StringContent(webhookPayload, Encoding.UTF8, "application/json"));
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await _factory.InDbAsync(db =>
            db.EmailLogs
                .Where(l => l.ProviderMessageId == messageId)
                .Select(l => l.Status)
                .FirstOrDefaultAsync());
        status.Should().Be(EmailStatus.Delivered);
    }

    [Fact]
    public async Task Invalid_unsubscribe_token_returns_404()
    {
        var client = _factory.CreateHttpsClient();
        var res = await client.GetAsync("/api/email/unsubscribe?token=invalid_token_here");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
