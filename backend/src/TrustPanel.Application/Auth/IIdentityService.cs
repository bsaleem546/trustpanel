namespace TrustPanel.Application.Auth;

/// <summary>Abstraction over ASP.NET Core Identity so Application stays framework-free.</summary>
public interface IIdentityService
{
    Task<(IdentityOperationResult Result, Guid UserId)> CreateUserAsync(
        string email, string password, CancellationToken cancellationToken);

    Task<string> GenerateEmailConfirmationTokenAsync(Guid userId);

    Task<IdentityOperationResult> ConfirmEmailAsync(Guid userId, string token);

    /// <summary>Returns the user when the email/password pair is valid, otherwise null.</summary>
    Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password);

    Task<AuthUserDto?> FindByIdAsync(Guid userId);

    Task<AuthUserDto?> FindByEmailAsync(string email);

    /// <summary>Returns null when no user exists for the email (caller must not reveal that).</summary>
    Task<string?> GeneratePasswordResetTokenAsync(string email);

    Task<IdentityOperationResult> ResetPasswordAsync(string email, string token, string newPassword);

    /// <summary>Finds or provisions a confirmed user for an external OAuth login.</summary>
    Task<AuthUserDto> FindOrCreateExternalUserAsync(string email);

    Task UpdateOnboardingAsync(Guid userId, string stateJson, bool completed);
}
