using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;
using TrustPanel.Application.Email;
using AppEmailMessage = TrustPanel.Application.Email.EmailMessage;

namespace TrustPanel.Infrastructure.Email;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly IResend _resend;
    private readonly string _defaultFrom;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(IResend resend, IConfiguration configuration, ILogger<ResendEmailSender> logger)
    {
        _resend = resend;
        _defaultFrom = configuration["RESEND_FROM_ADDRESS"] ?? "noreply@trustpanel.com";
        _logger = logger;
    }

    public async Task SendAsync(AppEmailMessage message, CancellationToken cancellationToken)
    {
        var from = message.FromAddress is not null
            ? (message.FromName is not null ? $"{message.FromName} <{message.FromAddress}>" : message.FromAddress)
            : _defaultFrom;

        var emailMessage = new Resend.EmailMessage
        {
            From = from,
            To = { message.To },
            Subject = message.Subject,
            HtmlBody = message.HtmlBody
        };

        try
        {
            await _resend.EmailSendAsync(emailMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", message.To);
            throw;
        }
    }
}
