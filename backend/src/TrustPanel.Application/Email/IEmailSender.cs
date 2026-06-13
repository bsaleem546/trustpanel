namespace TrustPanel.Application.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? FromName = null,
    string? FromAddress = null);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
