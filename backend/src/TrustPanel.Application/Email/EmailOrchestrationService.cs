using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Email;

namespace TrustPanel.Application.Email;

/// <summary>
/// Orchestrates email sending with suppression checks and per-address rate limiting.
/// Max 3 emails per address per 30 days, tracked via IRateLimiter.
/// </summary>
public sealed class EmailOrchestrationService
{
    private const int MaxEmailsPer30Days = 3;
    private static readonly TimeSpan Window = TimeSpan.FromDays(30);

    private readonly IAppDbContext _db;
    private readonly IEmailSender _sender;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly IRateLimiter _rateLimiter;

    public EmailOrchestrationService(
        IAppDbContext db, IEmailSender sender, IEmailTemplateRenderer renderer, IRateLimiter rateLimiter)
    {
        _db = db; _sender = sender; _renderer = renderer; _rateLimiter = rateLimiter;
    }

    public async Task SendAsync(
        Guid workspaceId,
        string toAddress,
        EmailTemplateType template,
        IReadOnlyDictionary<string, string> mergeFields,
        string? fromName = null,
        string? fromAddress = null,
        CancellationToken cancellationToken = default)
    {
        // Suppression check.
        var suppressed = await _db.EmailSuppressions
            .AnyAsync(s => s.Email == toAddress, cancellationToken);
        if (suppressed) return;

        // Rate cap: max 3 emails per address per 30 days.
        var rateLimitKey = $"email:{toAddress}:30d";
        var allowed = await _rateLimiter.TryConsumeAsync(
            rateLimitKey, MaxEmailsPer30Days, Window, cancellationToken);
        if (!allowed) return;

        var (subject, html) = await _renderer.RenderAsync(template, mergeFields, cancellationToken);

        var log = new EmailLog
        {
            WorkspaceId = workspaceId,
            Template = template,
            Recipient = toAddress,
            Status = EmailStatus.Sent,
            SentAt = DateTimeOffset.UtcNow
        };
        _db.EmailLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        await _sender.SendAsync(new EmailMessage(toAddress, subject, html, fromName, fromAddress),
            cancellationToken);
    }
}
