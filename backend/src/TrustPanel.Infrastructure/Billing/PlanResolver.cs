using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Billing;
using TrustPanel.Domain.Billing;
using TrustPanel.Infrastructure.Persistence;

namespace TrustPanel.Infrastructure.Billing;

public sealed class PlanResolver : IPlanResolver
{
    public const int TrialDays = 14;

    private readonly AppDbContext _db;

    public PlanResolver(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EffectivePlan> ResolveForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var overridePlanId = await _db.SuperAdminOverrides
            .Where(o => o.UserId == userId && (o.ExpiresAt == null || o.ExpiresAt > now))
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => (Guid?)o.PlanId)
            .FirstOrDefaultAsync(cancellationToken);

        if (overridePlanId is not null)
        {
            var overridePlan = await _db.Plans.FirstAsync(p => p.Id == overridePlanId, cancellationToken);
            return new EffectivePlan(overridePlan, IsTrial: false, TrialEndsAt: null);
        }

        var subscription = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId)
            .Where(s => s.Status == SubscriptionStatus.Active
                || s.Status == SubscriptionStatus.Trialing
                || (s.Status == SubscriptionStatus.PastDue
                    && s.GracePeriodEndsAt != null && s.GracePeriodEndsAt > now))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription?.Plan is not null)
        {
            return new EffectivePlan(subscription.Plan, IsTrial: false, TrialEndsAt: null);
        }

        var userCreatedAt = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => (DateTimeOffset?)u.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var trialEndsAt = (userCreatedAt ?? now).AddDays(TrialDays);
        if (trialEndsAt > now)
        {
            var trialPlan = await _db.Plans
                .FirstAsync(p => p.Code == PlanCodes.AgencyPlus, cancellationToken);
            return new EffectivePlan(trialPlan, IsTrial: true, trialEndsAt);
        }

        var starter = await _db.Plans.FirstAsync(p => p.Code == PlanCodes.Starter, cancellationToken);
        return new EffectivePlan(starter, IsTrial: true, trialEndsAt);
    }
}
