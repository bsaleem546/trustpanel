using Microsoft.Extensions.Logging;
using TrustPanel.Application.Email;

namespace TrustPanel.Infrastructure.Email;

/// <summary>Logs emails instead of sending when Resend is not configured.</summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Email] To: {To} | Subject: {Subject}",
            message.To, message.Subject);
        return Task.CompletedTask;
    }
}
