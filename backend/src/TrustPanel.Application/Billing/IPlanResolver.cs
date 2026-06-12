using TrustPanel.Domain.Billing;

namespace TrustPanel.Application.Billing;

public sealed record EffectivePlan(Plan Plan, bool IsTrial, DateTimeOffset? TrialEndsAt)
{
    public bool TrialExpired => IsTrial && TrialEndsAt is not null && TrialEndsAt < DateTimeOffset.UtcNow;
}

/// <summary>
/// Resolves the plan that currently governs a user's limits: super-admin override
/// first, then active subscription, then the 14-day full trial, then Starter limits
/// once the trial has lapsed (restrict, never delete).
/// </summary>
public interface IPlanResolver
{
    Task<EffectivePlan> ResolveForUserAsync(Guid userId, CancellationToken cancellationToken);
}
