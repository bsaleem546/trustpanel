namespace TrustPanel.Application.Auth;

public sealed record IdentityOperationResult(bool Succeeded, string[] Errors)
{
    public static IdentityOperationResult Success() => new(true, []);
    public static IdentityOperationResult Failure(params string[] errors) => new(false, errors);
}

public sealed record AuthUserDto(
    Guid Id,
    string Email,
    bool EmailConfirmed,
    string Role,
    bool OnboardingCompleted,
    string OnboardingState);

public sealed record AccessTokenDto(string Token, DateTimeOffset ExpiresAt);

public sealed record AuthResultDto(
    AccessTokenDto AccessToken,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid SessionId,
    AuthUserDto User,
    Guid? WorkspaceId);

public sealed record SessionDto(
    Guid SessionId,
    string UserAgent,
    string IpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsCurrent);
