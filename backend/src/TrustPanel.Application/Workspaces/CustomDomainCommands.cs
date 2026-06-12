using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Billing;
using TrustPanel.Application.Common;

namespace TrustPanel.Application.Workspaces;

public sealed record CustomDomainDto(
    string Domain, string CnameTarget, bool Verified, DateTimeOffset? VerifiedAt);

public sealed record SetCustomDomainCommand(Guid UserId, Guid WorkspaceId, string Domain)
    : IRequest<CustomDomainDto>;

public sealed class SetCustomDomainCommandValidator : AbstractValidator<SetCustomDomainCommand>
{
    public SetCustomDomainCommandValidator()
    {
        RuleFor(c => c.Domain)
            .NotEmpty()
            .MaximumLength(255)
            .Matches(@"^(?!-)[a-zA-Z0-9-]{1,63}(?<!-)(\.(?!-)[a-zA-Z0-9-]{1,63}(?<!-))+$")
            .WithMessage("Enter a valid domain such as reviews.yourbrand.com.");
    }
}

public sealed class SetCustomDomainCommandHandler
    : IRequestHandler<SetCustomDomainCommand, CustomDomainDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IPlanResolver _planResolver;
    private readonly CustomDomainOptions _options;

    public SetCustomDomainCommandHandler(
        IAppDbContext db,
        WorkspaceAccess access,
        IPlanResolver planResolver,
        CustomDomainOptions options)
    {
        _db = db;
        _access = access;
        _planResolver = planResolver;
        _options = options;
    }

    public async Task<CustomDomainDto> Handle(
        SetCustomDomainCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _access.RequireManageAsync(
            request.WorkspaceId, request.UserId, cancellationToken);

        var ownerPlan = await _planResolver.ResolveForUserAsync(
            workspace.OwnerUserId, cancellationToken);
        if (!ownerPlan.Plan.HasCustomDomain)
        {
            throw new ForbiddenAppException("Custom domains require the Agency plan.");
        }

        var domain = request.Domain.Trim().ToLowerInvariant();
        var taken = await _db.Workspaces.AnyAsync(
            w => w.CustomDomain == domain && w.Id != workspace.Id, cancellationToken);
        if (taken)
        {
            throw new ConflictException("That domain is already connected to another workspace.");
        }

        workspace.CustomDomain = domain;
        workspace.DomainVerifiedAt = null;
        workspace.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new CustomDomainDto(domain, _options.CnameTarget, Verified: false, VerifiedAt: null);
    }
}

public sealed record RemoveCustomDomainCommand(Guid UserId, Guid WorkspaceId) : IRequest;

public sealed class RemoveCustomDomainCommandHandler : IRequestHandler<RemoveCustomDomainCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public RemoveCustomDomainCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task Handle(RemoveCustomDomainCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _access.RequireManageAsync(
            request.WorkspaceId, request.UserId, cancellationToken);

        workspace.CustomDomain = null;
        workspace.DomainVerifiedAt = null;
        workspace.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record VerifyCustomDomainCommand(Guid UserId, Guid WorkspaceId)
    : IRequest<CustomDomainDto>;

public sealed class VerifyCustomDomainCommandHandler
    : IRequestHandler<VerifyCustomDomainCommand, CustomDomainDto>
{
    private readonly WorkspaceAccess _access;
    private readonly DomainVerificationService _verification;
    private readonly CustomDomainOptions _options;

    public VerifyCustomDomainCommandHandler(
        WorkspaceAccess access,
        DomainVerificationService verification,
        CustomDomainOptions options)
    {
        _access = access;
        _verification = verification;
        _options = options;
    }

    public async Task<CustomDomainDto> Handle(
        VerifyCustomDomainCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _access.RequireManageAsync(
            request.WorkspaceId, request.UserId, cancellationToken);

        if (string.IsNullOrWhiteSpace(workspace.CustomDomain))
        {
            throw new NotFoundException("This workspace has no custom domain configured.");
        }

        var verified = await _verification.VerifyAsync(workspace, cancellationToken);
        return new CustomDomainDto(
            workspace.CustomDomain, _options.CnameTarget, verified, workspace.DomainVerifiedAt);
    }
}
