using FluentValidation;
using MediatR;
using TrustPanel.Application.Billing;
using TrustPanel.Application.Common;

namespace TrustPanel.Application.Workspaces;

/// <summary>Partial update: null fields are left unchanged.</summary>
public sealed record UpdateBrandingCommand(
    Guid UserId,
    Guid WorkspaceId,
    string? LogoPath,
    string? PrimaryColor,
    string? SecondaryColor,
    string? FontFamily,
    bool? ShowTrustPanelBranding,
    string? EmailFromName,
    string? EmailFromAddress) : IRequest<WorkspaceDto>;

public sealed class UpdateBrandingCommandValidator : AbstractValidator<UpdateBrandingCommand>
{
    public UpdateBrandingCommandValidator()
    {
        RuleFor(c => c.LogoPath).MaximumLength(512);
        RuleFor(c => c.PrimaryColor).Matches("^#[0-9a-fA-F]{6}$")
            .When(c => c.PrimaryColor is not null)
            .WithMessage("Colors must be hex values like #7C6AF7.");
        RuleFor(c => c.SecondaryColor).Matches("^#[0-9a-fA-F]{6}$")
            .When(c => c.SecondaryColor is not null)
            .WithMessage("Colors must be hex values like #7C6AF7.");
        RuleFor(c => c.FontFamily).MaximumLength(64);
        RuleFor(c => c.EmailFromName).MaximumLength(128);
        RuleFor(c => c.EmailFromAddress).EmailAddress()
            .When(c => !string.IsNullOrEmpty(c.EmailFromAddress));
    }
}

public sealed class UpdateBrandingCommandHandler : IRequestHandler<UpdateBrandingCommand, WorkspaceDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IPlanResolver _planResolver;

    public UpdateBrandingCommandHandler(
        IAppDbContext db, WorkspaceAccess access, IPlanResolver planResolver)
    {
        _db = db;
        _access = access;
        _planResolver = planResolver;
    }

    public async Task<WorkspaceDto> Handle(
        UpdateBrandingCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _access.RequireManageAsync(
            request.WorkspaceId, request.UserId, cancellationToken);

        // White-label gates are governed by the workspace owner's plan.
        var ownerPlan = await _planResolver.ResolveForUserAsync(
            workspace.OwnerUserId, cancellationToken);

        if (request.ShowTrustPanelBranding == false && !ownerPlan.Plan.HasWhiteLabel)
        {
            throw new ForbiddenAppException(
                "Removing TrustPanel branding requires the Agency plan.");
        }

        var changesEmailSender = !string.IsNullOrEmpty(request.EmailFromName)
            || !string.IsNullOrEmpty(request.EmailFromAddress);
        if (changesEmailSender && !ownerPlan.Plan.HasCustomEmailSender)
        {
            throw new ForbiddenAppException(
                "A custom email sender requires the Agency plan.");
        }

        var branding = workspace.Branding;
        branding.LogoPath = request.LogoPath ?? branding.LogoPath;
        branding.PrimaryColor = request.PrimaryColor ?? branding.PrimaryColor;
        branding.SecondaryColor = request.SecondaryColor ?? branding.SecondaryColor;
        branding.FontFamily = request.FontFamily ?? branding.FontFamily;
        branding.ShowTrustPanelBranding =
            request.ShowTrustPanelBranding ?? branding.ShowTrustPanelBranding;

        workspace.EmailFrom.FromName = request.EmailFromName ?? workspace.EmailFrom.FromName;
        workspace.EmailFrom.FromEmail = request.EmailFromAddress ?? workspace.EmailFrom.FromEmail;
        workspace.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return WorkspaceDto.From(workspace, request.UserId);
    }
}
