using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Teams;
using TrustPanel.Domain.Workspaces;

namespace TrustPanel.Application.Workspaces;

/// <summary>Workspace-level access checks shared by workspace commands and queries.</summary>
public sealed class WorkspaceAccess
{
    private readonly IAppDbContext _db;

    public WorkspaceAccess(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Workspace> RequireManageAsync(
        Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var workspace = await FindAccessibleAsync(workspaceId, userId, cancellationToken)
            ?? throw new NotFoundException("Workspace not found.");

        if (workspace.OwnerUserId == userId)
        {
            return workspace;
        }

        var role = await MemberRoleAsync(workspaceId, userId, cancellationToken);
        if (role is not (WorkspaceRole.Owner or WorkspaceRole.Admin))
        {
            throw new ForbiddenAppException("You need admin access to manage this workspace.");
        }

        return workspace;
    }

    public async Task<Workspace> RequireOwnerAsync(
        Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var workspace = await FindAccessibleAsync(workspaceId, userId, cancellationToken)
            ?? throw new NotFoundException("Workspace not found.");

        if (workspace.OwnerUserId != userId)
        {
            throw new ForbiddenAppException("Only the workspace owner can perform this action.");
        }

        return workspace;
    }

    public async Task<Workspace> RequireMemberAsync(
        Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        return await FindAccessibleAsync(workspaceId, userId, cancellationToken)
            ?? throw new NotFoundException("Workspace not found.");
    }

    private async Task<Workspace?> FindAccessibleAsync(
        Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        var workspace = await _db.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace is null)
        {
            return null;
        }

        if (workspace.OwnerUserId == userId)
        {
            return workspace;
        }

        var isMember = await _db.WorkspaceMembers.AnyAsync(
            m => m.WorkspaceId == workspaceId && m.UserId == userId && m.AcceptedAt != null,
            cancellationToken);

        // Hide existence from non-members.
        return isMember ? workspace : null;
    }

    private Task<WorkspaceRole?> MemberRoleAsync(
        Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        return _db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.AcceptedAt != null)
            .Select(m => (WorkspaceRole?)m.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
