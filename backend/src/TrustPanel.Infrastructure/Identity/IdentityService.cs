using Microsoft.AspNetCore.Identity;
using TrustPanel.Application.Auth;

namespace TrustPanel.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<(IdentityOperationResult Result, Guid UserId)> CreateUserAsync(
        string email, string password, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(user, password);
        return (ToOperationResult(result), user.Id);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");
        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<IdentityOperationResult> ConfirmEmailAsync(Guid userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return IdentityOperationResult.Failure("User not found.");
        }

        return ToOperationResult(await _userManager.ConfirmEmailAsync(user, token));
    }

    public async Task<AuthUserDto?> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return null;
        }

        return ToDto(user);
    }

    public async Task<AuthUserDto?> FindByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToDto(user);
    }

    public async Task<AuthUserDto?> FindByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : ToDto(user);
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<IdentityOperationResult> ResetPasswordAsync(
        string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return IdentityOperationResult.Failure("Invalid reset request.");
        }

        return ToOperationResult(await _userManager.ResetPasswordAsync(user, token, newPassword));
    }

    public async Task<AuthUserDto> FindOrCreateExternalUserAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return ToDto(user);
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            // The OAuth provider has already verified ownership of the address.
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to provision external user: "
                + string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return ToDto(user);
    }

    public async Task UpdateOnboardingAsync(Guid userId, string stateJson, bool completed)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User {userId} not found.");

        user.OnboardingState = stateJson;
        user.OnboardingCompleted = completed;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);
    }

    private static AuthUserDto ToDto(ApplicationUser user) => new(
        user.Id,
        user.Email ?? string.Empty,
        user.EmailConfirmed,
        user.Role.ToString(),
        user.OnboardingCompleted,
        user.OnboardingState);

    private static IdentityOperationResult ToOperationResult(IdentityResult result) =>
        result.Succeeded
            ? IdentityOperationResult.Success()
            : IdentityOperationResult.Failure(result.Errors.Select(e => e.Description).ToArray());
}
