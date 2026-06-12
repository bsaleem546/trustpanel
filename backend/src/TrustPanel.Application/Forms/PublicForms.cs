using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Forms;
using TrustPanel.Domain.Workspaces;

namespace TrustPanel.Application.Forms;

/// <summary>
/// Reads a public collection form for an anonymous visitor. The workspace comes from
/// either a host-resolved workspace ID (custom domain) or the workspace slug in the URL.
/// </summary>
public sealed record GetPublicFormQuery(Guid? WorkspaceId, string? WorkspaceSlug, string FormSlug)
    : IRequest<PublicFormDto>;

public sealed class GetPublicFormQueryHandler : IRequestHandler<GetPublicFormQuery, PublicFormDto>
{
    private readonly IAppDbContext _db;

    public GetPublicFormQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PublicFormDto> Handle(
        GetPublicFormQuery request, CancellationToken cancellationToken)
    {
        var workspace = await PublicWorkspaceResolver.ResolveAsync(
            _db, request.WorkspaceId, request.WorkspaceSlug, cancellationToken);

        var form = await _db.CollectionForms.FirstOrDefaultAsync(
            f => f.WorkspaceId == workspace.Id && f.Slug == request.FormSlug && f.IsActive,
            cancellationToken)
            ?? throw new NotFoundException("Form not found.");

        return ToPublicDto(form, workspace);
    }

    internal static PublicFormDto ToPublicDto(CollectionForm form, Workspace workspace) => new(
        form.Id,
        workspace.Id,
        form.Slug,
        form.Name,
        form.AllowedSubmissionType,
        QuestionConfigDto.From(form.QuestionConfig),
        workspace.Name,
        workspace.Branding.LogoPath,
        workspace.Branding.PrimaryColor,
        workspace.Branding.SecondaryColor,
        workspace.Branding.FontFamily,
        workspace.Branding.ShowTrustPanelBranding);
}

internal static class PublicWorkspaceResolver
{
    public static async Task<Workspace> ResolveAsync(
        IAppDbContext db, Guid? workspaceId, string? workspaceSlug, CancellationToken cancellationToken)
    {
        Workspace? workspace = null;
        if (workspaceId is not null)
        {
            workspace = await db.Workspaces
                .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(workspaceSlug))
        {
            workspace = await db.Workspaces
                .FirstOrDefaultAsync(w => w.Slug == workspaceSlug, cancellationToken);
        }

        return workspace ?? throw new NotFoundException("Form not found.");
    }
}
