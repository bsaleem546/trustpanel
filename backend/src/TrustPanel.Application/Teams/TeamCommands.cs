using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TrustPanel.Application.Common;
using TrustPanel.Application.Common.Behaviors;
using TrustPanel.Application.Email;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Teams;

namespace TrustPanel.Application.Teams;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record TeamMemberDto(
    Guid Id, Guid? UserId, string? Email, WorkspaceRole Role,
    bool Accepted, DateTimeOffset CreatedAt);

// ── List members ──────────────────────────────────────────────────────────────

public sealed record ListTeamMembersQuery(Guid UserId, Guid WorkspaceId)
    : IRequest<IReadOnlyList<TeamMemberDto>>;

public sealed class ListTeamMembersQueryHandler
    : IRequestHandler<ListTeamMembersQuery, IReadOnlyList<TeamMemberDto>>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public ListTeamMembersQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<IReadOnlyList<TeamMemberDto>> Handle(
        ListTeamMembersQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireMemberAsync(request.WorkspaceId, request.UserId, cancellationToken);
        return await _db.WorkspaceMembers
            .Where(m => m.WorkspaceId == request.WorkspaceId)
            .Select(m => new TeamMemberDto(
                m.Id, m.UserId, m.InvitedEmail, m.Role,
                m.AcceptedAt != null, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

// ── Invite member ─────────────────────────────────────────────────────────────

public sealed record InviteMemberCommand(
    Guid UserId, Guid WorkspaceId, string Email, WorkspaceRole Role)
    : IRequest<string>; // returns invite token (plaintext, for email link)

public sealed class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Role).NotEqual(WorkspaceRole.Owner)
            .WithMessage("Cannot invite as Owner.");
    }
}

public sealed class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, string>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IEmailSender _email;
    private readonly IAuditTrail _audit;

    public InviteMemberCommandHandler(
        IAppDbContext db, WorkspaceAccess access, IEmailSender email, IAuditTrail audit)
    {
        _db = db; _access = access; _email = email; _audit = audit;
    }

    public async Task<string> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var existing = await _db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == request.WorkspaceId
                        && m.InvitedEmail == request.Email.ToLowerInvariant()
                        && m.AcceptedAt == null, cancellationToken);
        if (existing) throw new ConflictException("An active invitation for this email already exists.");

        var token = GenerateToken();
        var hash = HashToken(token);

        var member = new WorkspaceMember
        {
            WorkspaceId = request.WorkspaceId,
            InvitedEmail = request.Email.ToLowerInvariant(),
            Role = request.Role,
            InvitationTokenHash = hash,
            InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        _db.WorkspaceMembers.Add(member);
        _audit.Record(request.WorkspaceId, request.UserId, "InviteMember",
            "WorkspaceMember", member.Id, new { request.Email, request.Role });
        await _db.SaveChangesAsync(cancellationToken);

        return token;
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// ── Accept invitation ─────────────────────────────────────────────────────────

public sealed record AcceptInvitationCommand(string Token, Guid UserId, string UserEmail)
    : IRequest<Guid>; // returns workspaceId

public sealed class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, Guid>
{
    private readonly IAppDbContext _db;

    public AcceptInvitationCommandHandler(IAppDbContext db) { _db = db; }

    public async Task<Guid> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var hash = InviteMemberCommandHandler.HashToken(request.Token);
        var member = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m =>
                m.InvitationTokenHash == hash
                && m.AcceptedAt == null
                && m.InvitationExpiresAt > DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new NotFoundException("Invitation not found or has expired.");

        if (!string.Equals(member.InvitedEmail, request.UserEmail,
                StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAppException("This invitation was sent to a different email address.");

        member.UserId = request.UserId;
        member.AcceptedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return member.WorkspaceId;
    }
}

// ── Change role ───────────────────────────────────────────────────────────────

public sealed record ChangeMemberRoleCommand(
    Guid UserId, Guid WorkspaceId, Guid MemberId, WorkspaceRole NewRole)
    : IRequest;

public sealed class ChangeMemberRoleCommandHandler : IRequestHandler<ChangeMemberRoleCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IAuditTrail _audit;

    public ChangeMemberRoleCommandHandler(IAppDbContext db, WorkspaceAccess access, IAuditTrail audit)
    {
        _db = db; _access = access; _audit = audit;
    }

    public async Task Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var member = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.Id == request.MemberId
                                   && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        if (member.Role == WorkspaceRole.Owner)
            throw new ForbiddenAppException("Cannot change the owner's role.");

        if (request.NewRole == WorkspaceRole.Owner)
            throw new ForbiddenAppException("Use the transfer-ownership flow to assign Owner.");

        member.Role = request.NewRole;
        _audit.Record(request.WorkspaceId, request.UserId, "ChangeMemberRole",
            "WorkspaceMember", request.MemberId, new { request.NewRole });
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ── Remove member ─────────────────────────────────────────────────────────────

public sealed record RemoveMemberCommand(Guid UserId, Guid WorkspaceId, Guid MemberId)
    : IRequest;

public sealed class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IAuditTrail _audit;

    public RemoveMemberCommandHandler(IAppDbContext db, WorkspaceAccess access, IAuditTrail audit)
    {
        _db = db; _access = access; _audit = audit;
    }

    public async Task Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var member = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.Id == request.MemberId
                                   && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("Member not found.");

        if (member.Role == WorkspaceRole.Owner)
            throw new ForbiddenAppException("Cannot remove the workspace owner.");

        _db.WorkspaceMembers.Remove(member);
        _audit.Record(request.WorkspaceId, request.UserId, "RemoveMember",
            "WorkspaceMember", request.MemberId);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
