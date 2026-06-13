using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Auth;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Users;

namespace TrustPanel.Application.Admin;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record AdminUserDto(Guid Id, string Email, bool IsSuperAdmin, DateTimeOffset CreatedAt);
public sealed record AdminWorkspaceDto(Guid Id, string Name, string Slug, Guid OwnerId, DateTimeOffset CreatedAt);
public sealed record AdminOverrideDto(Guid Id, Guid UserId, Guid PlanId, string Reason, DateTimeOffset? ExpiresAt, DateTimeOffset CreatedAt);

// ── List workspaces ───────────────────────────────────────────────────────────

public sealed record ListAdminWorkspacesQuery(int Page = 1, int PageSize = 50)
    : IRequest<IReadOnlyList<AdminWorkspaceDto>>;

public sealed class ListAdminWorkspacesQueryHandler
    : IRequestHandler<ListAdminWorkspacesQuery, IReadOnlyList<AdminWorkspaceDto>>
{
    private readonly IAppDbContext _db;

    public ListAdminWorkspacesQueryHandler(IAppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<AdminWorkspaceDto>> Handle(
        ListAdminWorkspacesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Workspaces
            .OrderByDescending(w => w.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(w => new AdminWorkspaceDto(w.Id, w.Name, w.Slug, w.OwnerUserId, w.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

// ── List plan overrides ───────────────────────────────────────────────────────

public sealed record ListPlanOverridesQuery : IRequest<IReadOnlyList<AdminOverrideDto>>;

public sealed class ListPlanOverridesQueryHandler
    : IRequestHandler<ListPlanOverridesQuery, IReadOnlyList<AdminOverrideDto>>
{
    private readonly IAppDbContext _db;

    public ListPlanOverridesQueryHandler(IAppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<AdminOverrideDto>> Handle(
        ListPlanOverridesQuery request, CancellationToken cancellationToken)
    {
        return await _db.SuperAdminOverrides
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new AdminOverrideDto(o.Id, o.UserId, o.PlanId,
                o.Reason, o.ExpiresAt, o.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

// ── Impersonate user ──────────────────────────────────────────────────────────

public sealed record ImpersonateUserCommand(Guid AdminUserId, Guid TargetUserId)
    : IRequest<string>; // returns short-lived JWT

public interface IAdminUserLookup
{
    Task<(string Email, Guid? WorkspaceId)?> FindUserAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class ImpersonateUserCommandHandler : IRequestHandler<ImpersonateUserCommand, string>
{
    private readonly IAdminUserLookup _lookup;
    private readonly ITokenService _tokens;

    public ImpersonateUserCommandHandler(IAdminUserLookup lookup, ITokenService tokens)
    {
        _lookup = lookup; _tokens = tokens;
    }

    public async Task<string> Handle(ImpersonateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _lookup.FindUserAsync(request.TargetUserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var token = _tokens.CreateAccessToken(
            request.TargetUserId, user.Email, "User", user.WorkspaceId,
            Guid.NewGuid(), request.AdminUserId);
        return token.Token;
    }
}

// ── Plan override CRUD ────────────────────────────────────────────────────────

public sealed record CreatePlanOverrideCommand(
    Guid AdminUserId, Guid TargetUserId, Guid PlanId,
    string Reason, DateTimeOffset? ExpiresAt)
    : IRequest<AdminOverrideDto>;

public sealed class CreatePlanOverrideCommandHandler
    : IRequestHandler<CreatePlanOverrideCommand, AdminOverrideDto>
{
    private readonly IAppDbContext _db;

    public CreatePlanOverrideCommandHandler(IAppDbContext db) { _db = db; }

    public async Task<AdminOverrideDto> Handle(
        CreatePlanOverrideCommand request, CancellationToken cancellationToken)
    {
        // Remove any existing active override for the user.
        var existing = await _db.SuperAdminOverrides
            .Where(o => o.UserId == request.TargetUserId)
            .ToListAsync(cancellationToken);
        foreach (var o in existing) _db.SuperAdminOverrides.Remove(o);

        var override_ = new SuperAdminOverride
        {
            UserId = request.TargetUserId,
            PlanId = request.PlanId,
            Reason = request.Reason,
            ExpiresAt = request.ExpiresAt,
            CreatedByUserId = request.AdminUserId
        };
        _db.SuperAdminOverrides.Add(override_);
        await _db.SaveChangesAsync(cancellationToken);

        return new AdminOverrideDto(override_.Id, override_.UserId, override_.PlanId,
            override_.Reason, override_.ExpiresAt, override_.CreatedAt);
    }
}

public sealed record DeletePlanOverrideCommand(Guid OverrideId) : IRequest;

public sealed class DeletePlanOverrideCommandHandler : IRequestHandler<DeletePlanOverrideCommand>
{
    private readonly IAppDbContext _db;

    public DeletePlanOverrideCommandHandler(IAppDbContext db) { _db = db; }

    public async Task Handle(DeletePlanOverrideCommand request, CancellationToken cancellationToken)
    {
        var override_ = await _db.SuperAdminOverrides
            .FirstOrDefaultAsync(o => o.Id == request.OverrideId, cancellationToken)
            ?? throw new NotFoundException("Override not found.");
        _db.SuperAdminOverrides.Remove(override_);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
