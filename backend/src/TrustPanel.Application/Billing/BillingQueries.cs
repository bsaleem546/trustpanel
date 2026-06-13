using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Billing;

namespace TrustPanel.Application.Billing;

public sealed record WorkspacePlanDto(
    string PlanName,
    string PlanCode,
    decimal MonthlyPrice,
    SubscriptionStatus? Status,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd,
    bool IsTrialing,
    int TestimonialLimit,
    int WidgetLimit,
    bool HasVideoTestimonials,
    bool HasAiFeatures,
    bool HasApiAccess,
    bool HasWhiteLabel,
    bool HasCustomDomain,
    bool HasTeamMembers);

public sealed record GetWorkspacePlanQuery(Guid UserId, Guid WorkspaceId) : IRequest<WorkspacePlanDto>;

public sealed class GetWorkspacePlanQueryHandler : IRequestHandler<GetWorkspacePlanQuery, WorkspacePlanDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IPlanResolver _plan;

    public GetWorkspacePlanQueryHandler(IAppDbContext db, WorkspaceAccess access, IPlanResolver plan)
    {
        _db = db; _access = access; _plan = plan;
    }

    public async Task<WorkspacePlanDto> Handle(GetWorkspacePlanQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _access.RequireMemberAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var subscription = await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == workspace.OwnerUserId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var effective = await _plan.ResolveForUserAsync(workspace.OwnerUserId, cancellationToken);
        var plan = effective.Plan;

        return new WorkspacePlanDto(
            plan.Name,
            plan.Code,
            plan.MonthlyPrice,
            subscription?.Status,
            subscription?.CurrentPeriodEnd,
            subscription?.CancelAtPeriodEnd ?? false,
            effective.IsTrial,
            plan.TestimonialLimit,
            plan.WidgetLimit,
            plan.HasVideoTestimonials,
            plan.HasAiFeatures,
            plan.HasApiAccess,
            plan.HasWhiteLabel,
            plan.HasCustomDomain,
            plan.HasTeamMembers);
    }
}
