using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Billing;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Teams;

namespace TrustPanel.Application.Workspaces;

public sealed record ListWorkspacesQuery(Guid UserId) : IRequest<IReadOnlyList<WorkspaceDto>>;

public sealed class ListWorkspacesQueryHandler
    : IRequestHandler<ListWorkspacesQuery, IReadOnlyList<WorkspaceDto>>
{
    private readonly IAppDbContext _db;

    public ListWorkspacesQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WorkspaceDto>> Handle(
        ListWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var memberWorkspaceIds = _db.WorkspaceMembers
            .Where(m => m.UserId == request.UserId && m.AcceptedAt != null)
            .Select(m => m.WorkspaceId);

        var workspaces = await _db.Workspaces
            .Where(w => w.OwnerUserId == request.UserId || memberWorkspaceIds.Contains(w.Id))
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        return workspaces.Select(w => WorkspaceDto.From(w, request.UserId)).ToList();
    }
}

public sealed record GetWorkspaceQuery(Guid UserId, Guid WorkspaceId) : IRequest<WorkspaceDto>;

public sealed class GetWorkspaceQueryHandler : IRequestHandler<GetWorkspaceQuery, WorkspaceDto>
{
    private readonly WorkspaceAccess _access;

    public GetWorkspaceQueryHandler(WorkspaceAccess access)
    {
        _access = access;
    }

    public async Task<WorkspaceDto> Handle(GetWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _access.RequireMemberAsync(
            request.WorkspaceId, request.UserId, cancellationToken);
        return WorkspaceDto.From(workspace, request.UserId);
    }
}

public sealed record CreateWorkspaceCommand(Guid UserId, string Name) : IRequest<WorkspaceDto>;

public sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, WorkspaceDto>
{
    private readonly IAppDbContext _db;
    private readonly IPlanResolver _planResolver;

    public CreateWorkspaceCommandHandler(IAppDbContext db, IPlanResolver planResolver)
    {
        _db = db;
        _planResolver = planResolver;
    }

    public async Task<WorkspaceDto> Handle(
        CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var effectivePlan = await _planResolver.ResolveForUserAsync(request.UserId, cancellationToken);
        var limit = effectivePlan.Plan.WorkspaceLimit;

        if (limit >= 0)
        {
            var owned = await _db.Workspaces
                .CountAsync(w => w.OwnerUserId == request.UserId, cancellationToken);
            if (owned >= limit)
            {
                throw new ForbiddenAppException(
                    $"Your {effectivePlan.Plan.Name} plan allows {limit} workspace(s). "
                    + "Upgrade to add more.");
            }
        }

        var (workspace, membership) = WorkspaceFactory.CreateDefault(
            request.UserId, "owner@workspace", request.Name);
        _db.Workspaces.Add(workspace);
        _db.WorkspaceMembers.Add(membership);
        await _db.SaveChangesAsync(cancellationToken);

        return WorkspaceDto.From(workspace, request.UserId);
    }
}

public sealed record UpdateWorkspaceCommand(Guid UserId, Guid WorkspaceId, string Name)
    : IRequest<WorkspaceDto>;

public sealed class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, WorkspaceDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public UpdateWorkspaceCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<WorkspaceDto> Handle(
        UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _access.RequireManageAsync(
            request.WorkspaceId, request.UserId, cancellationToken);

        workspace.Name = request.Name;
        workspace.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return WorkspaceDto.From(workspace, request.UserId);
    }
}

public sealed record DeleteWorkspaceCommand(Guid UserId, Guid WorkspaceId) : IRequest;

public sealed class DeleteWorkspaceCommandHandler : IRequestHandler<DeleteWorkspaceCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public DeleteWorkspaceCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _access.RequireOwnerAsync(
            request.WorkspaceId, request.UserId, cancellationToken);

        var memberships = await _db.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspace.Id)
            .ToListAsync(cancellationToken);

        _db.WorkspaceMembers.RemoveRange(memberships);
        _db.Workspaces.Remove(workspace);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
