using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Email;

namespace TrustPanel.Application.Email;

public sealed record SendTestimonialRequestCommand(
    Guid UserId,
    Guid WorkspaceId,
    string RecipientName,
    string RecipientEmail,
    string? FormId,
    string? CustomMessage) : IRequest;

public sealed class SendTestimonialRequestCommandHandler : IRequestHandler<SendTestimonialRequestCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly EmailOrchestrationService _email;

    public SendTestimonialRequestCommandHandler(
        IAppDbContext db, WorkspaceAccess access, EmailOrchestrationService email)
    {
        _db = db; _access = access; _email = email;
    }

    public async Task Handle(SendTestimonialRequestCommand request, CancellationToken cancellationToken)
    {
        var workspace = await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        // Build the collection link.
        string collectionUrl;
        if (request.FormId is not null && Guid.TryParse(request.FormId, out var formId))
        {
            var form = await _db.CollectionForms
                .FirstOrDefaultAsync(f => f.Id == formId && f.WorkspaceId == request.WorkspaceId, cancellationToken);
            collectionUrl = form is not null
                ? $"https://trustpanel.io/c/{workspace.Slug}/{form.Slug}"
                : $"https://trustpanel.io/c/{workspace.Slug}";
        }
        else
        {
            collectionUrl = $"https://trustpanel.io/c/{workspace.Slug}";
        }

        var mergeFields = new Dictionary<string, string>
        {
            ["RecipientName"] = request.RecipientName,
            ["WorkspaceName"] = workspace.Name,
            ["CollectionUrl"] = collectionUrl,
            ["CustomMessage"] = request.CustomMessage ?? string.Empty,
        };

        await _email.SendAsync(
            request.WorkspaceId,
            request.RecipientEmail,
            EmailTemplateType.TestimonialRequest,
            mergeFields,
            cancellationToken: cancellationToken);
    }
}
