using Microsoft.Extensions.Logging;
using TrustPanel.Application.Auth;

namespace TrustPanel.Infrastructure.Email;

/// <summary>
/// Placeholder until the email system phase delivers through Resend + Hangfire.
/// Logs instead of sending so auth flows are fully exercisable in development.
/// </summary>
public sealed class LoggingAuthEmailSender : IAuthEmailSender
{
    private readonly ILogger<LoggingAuthEmailSender> _logger;

    public LoggingAuthEmailSender(ILogger<LoggingAuthEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailConfirmationAsync(
        string email, Guid userId, string token, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Email confirmation requested for {Email} (user {UserId}). Token: {Token}",
            email, userId, token);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Password reset requested for {Email}. Token: {Token}", email, token);
        return Task.CompletedTask;
    }
}
