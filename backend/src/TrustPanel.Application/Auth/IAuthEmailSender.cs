namespace TrustPanel.Application.Auth;

/// <summary>
/// Sends auth lifecycle emails. Implemented as a logging stub until the email
/// system phase wires Resend + Hangfire delivery.
/// </summary>
public interface IAuthEmailSender
{
    Task SendEmailConfirmationAsync(string email, Guid userId, string token, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken);
}
