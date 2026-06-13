using TrustPanel.Application.Email;
using TrustPanel.Domain.Email;

namespace TrustPanel.Infrastructure.Email;

/// <summary>
/// Simple string-interpolation-based email template renderer.
/// Replaces {{Key}} placeholders with merge field values.
/// </summary>
public sealed class SimpleTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Dictionary<EmailTemplateType, (string Subject, string Html)> Templates = new()
    {
        [EmailTemplateType.TestimonialRequest] = (
            "Share your experience with {{WorkspaceName}}",
            "<p>Hi {{SubmitterName}},</p><p>We'd love to hear your feedback. <a href='{{FormLink}}'>Share your testimonial</a>.</p><p>Thank you,<br/>{{WorkspaceName}}</p>"),
        [EmailTemplateType.FollowUp] = (
            "A quick follow-up from {{WorkspaceName}}",
            "<p>Hi {{SubmitterName}},</p><p>We noticed you haven't shared your feedback yet. <a href='{{FormLink}}'>Share your testimonial</a>.</p><p>Thank you,<br/>{{WorkspaceName}}</p>"),
        [EmailTemplateType.ThankYou] = (
            "Thank you for your testimonial!",
            "<p>Hi {{SubmitterName}},</p><p>Thank you so much for sharing your experience with {{WorkspaceName}}. We really appreciate it!</p>"),
        [EmailTemplateType.NewTestimonialNotification] = (
            "New testimonial received",
            "<p>A new testimonial has been submitted to your workspace <strong>{{WorkspaceName}}</strong>.</p><p>Submitter: {{SubmitterName}}</p>"),
        [EmailTemplateType.WeeklyDigest] = (
            "Your weekly testimonials digest",
            "<p>Hi,</p><p>Here's your weekly digest for {{WorkspaceName}}:</p><p>{{DigestContent}}</p>"),
        [EmailTemplateType.EmailConfirmation] = (
            "Confirm your email",
            "<p>Please <a href='{{ConfirmationLink}}'>confirm your email address</a>.</p>"),
        [EmailTemplateType.PasswordReset] = (
            "Reset your password",
            "<p>Click <a href='{{ResetLink}}'>here</a> to reset your password. This link expires in 60 minutes.</p>"),
        [EmailTemplateType.TeamInvitation] = (
            "You've been invited to {{WorkspaceName}}",
            "<p>Hi,</p><p>You've been invited to join <strong>{{WorkspaceName}}</strong>. <a href='{{InviteLink}}'>Accept invitation</a>.</p>"),
        [EmailTemplateType.Broadcast] = (
            "{{BroadcastSubject}}",
            "{{BroadcastBody}}")
    };

    public Task<(string Subject, string Html)> RenderAsync(
        EmailTemplateType template,
        IReadOnlyDictionary<string, string> mergeFields,
        CancellationToken cancellationToken)
    {
        if (!Templates.TryGetValue(template, out var tpl))
            return Task.FromResult(("Notification", "<p>You have a new notification.</p>"));

        var subject = ApplyMergeFields(tpl.Subject, mergeFields);
        var html = ApplyMergeFields(tpl.Html, mergeFields);
        return Task.FromResult((subject, html));
    }

    private static string ApplyMergeFields(string template, IReadOnlyDictionary<string, string> fields)
    {
        foreach (var (key, value) in fields)
            template = template.Replace($"{{{{{key}}}}}", value, StringComparison.Ordinal);
        return template;
    }
}
