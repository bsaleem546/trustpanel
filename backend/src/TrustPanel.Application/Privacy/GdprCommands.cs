using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;

namespace TrustPanel.Application.Privacy;

public sealed record GdprExportDto(
    Guid TestimonialId, string? SubmitterName, string? SubmitterEmail,
    string? SubmitterCompany, string? Content, int? Rating, DateTimeOffset CreatedAt);

public sealed record GetGdprExportQuery(Guid UserId, Guid WorkspaceId)
    : IRequest<IReadOnlyList<GdprExportDto>>;

public sealed class GetGdprExportQueryHandler
    : IRequestHandler<GetGdprExportQuery, IReadOnlyList<GdprExportDto>>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public GetGdprExportQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<IReadOnlyList<GdprExportDto>> Handle(
        GetGdprExportQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);
        return await _db.Testimonials
            .Where(t => t.WorkspaceId == request.WorkspaceId && t.Submitter != null)
            .Select(t => new GdprExportDto(
                t.Id,
                t.Submitter!.Name,
                t.Submitter.Email,
                t.Submitter.Company,
                t.Content,
                t.Rating,
                t.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

public sealed record GdprDeleteCommand(Guid UserId, Guid WorkspaceId, string SubmitterEmail)
    : IRequest<int>; // returns count deleted

public sealed class GdprDeleteCommandHandler : IRequestHandler<GdprDeleteCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public GdprDeleteCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<int> Handle(GdprDeleteCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var email = request.SubmitterEmail;
        // Submitter is a JSON-owned type; filter client-side after loading workspace's testimonials.
        var all = await _db.Testimonials
            .Where(t => t.WorkspaceId == request.WorkspaceId && t.Submitter != null)
            .ToListAsync(cancellationToken);
        var testimonials = all
            .Where(t => string.Equals(t.Submitter?.Email, email, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var t in testimonials)
        {
            // Purge personal fields but keep the testimonial for aggregate analytics.
            if (t.Submitter is not null)
            {
                t.Submitter.Name = "[deleted]";
                t.Submitter.Email = null;
                t.Submitter.Company = null;
                t.Submitter.JobTitle = null;
                t.Submitter.AvatarPath = null;
            }
            t.Content = "[redacted]";
        }

        await _db.SaveChangesAsync(cancellationToken);
        return testimonials.Count;
    }
}
