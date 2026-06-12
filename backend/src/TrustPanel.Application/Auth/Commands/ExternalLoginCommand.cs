using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;

namespace TrustPanel.Application.Auth.Commands;

/// <summary>Completes a Google (or other OAuth) sign-in for a verified external email.</summary>
public sealed record ExternalLoginCommand(string Email, string UserAgent, string IpAddress)
    : IRequest<AuthResultDto>;

public sealed class ExternalLoginCommandValidator : AbstractValidator<ExternalLoginCommand>
{
    public ExternalLoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}

public sealed class ExternalLoginCommandHandler : IRequestHandler<ExternalLoginCommand, AuthResultDto>
{
    private readonly IIdentityService _identityService;
    private readonly IAppDbContext _db;
    private readonly AuthSessionService _sessions;

    public ExternalLoginCommandHandler(
        IIdentityService identityService, IAppDbContext db, AuthSessionService sessions)
    {
        _identityService = identityService;
        _db = db;
        _sessions = sessions;
    }

    public async Task<AuthResultDto> Handle(
        ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.FindOrCreateExternalUserAsync(request.Email);

        var hasWorkspace = await _db.Workspaces
            .AnyAsync(w => w.OwnerUserId == user.Id, cancellationToken);
        if (!hasWorkspace)
        {
            var (workspace, membership) = WorkspaceFactory.CreateDefault(user.Id, user.Email);
            _db.Workspaces.Add(workspace);
            _db.WorkspaceMembers.Add(membership);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await _sessions.IssueAsync(user, request.UserAgent, request.IpAddress, cancellationToken);
    }
}
