using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Integrations;

namespace TrustPanel.Application.Integrations;

public sealed record ImportSourceDto(
    Guid Id,
    Guid WorkspaceId,
    ImportProvider Provider,
    string? ExternalAccountId,
    DateTimeOffset? LastSyncedAt,
    bool IsActive,
    DateTimeOffset CreatedAt)
{
    public static ImportSourceDto From(ImportSource s) => new(
        s.Id, s.WorkspaceId, s.Provider, s.ExternalAccountId,
        s.LastSyncedAt, s.IsActive, s.CreatedAt);
}

// ── List ──────────────────────────────────────────────────────────────────────

public sealed record ListImportSourcesQuery(Guid UserId, Guid WorkspaceId)
    : IRequest<IReadOnlyList<ImportSourceDto>>;

public sealed class ListImportSourcesQueryHandler
    : IRequestHandler<ListImportSourcesQuery, IReadOnlyList<ImportSourceDto>>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public ListImportSourcesQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<IReadOnlyList<ImportSourceDto>> Handle(
        ListImportSourcesQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireMemberAsync(request.WorkspaceId, request.UserId, cancellationToken);
        var sources = await _db.ImportSources
            .Where(s => s.WorkspaceId == request.WorkspaceId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
        return sources.Select(ImportSourceDto.From).ToList();
    }
}

// ── Create ────────────────────────────────────────────────────────────────────

public sealed record CreateImportSourceCommand(
    Guid UserId, Guid WorkspaceId, ImportProvider Provider, string? ExternalAccountId)
    : IRequest<ImportSourceDto>;

public sealed class CreateImportSourceCommandHandler
    : IRequestHandler<CreateImportSourceCommand, ImportSourceDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public CreateImportSourceCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<ImportSourceDto> Handle(
        CreateImportSourceCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var source = new ImportSource
        {
            WorkspaceId = request.WorkspaceId,
            Provider = request.Provider,
            ExternalAccountId = request.ExternalAccountId
        };

        _db.ImportSources.Add(source);
        await _db.SaveChangesAsync(cancellationToken);
        return ImportSourceDto.From(source);
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

public sealed record DeleteImportSourceCommand(Guid UserId, Guid ImportSourceId) : IRequest;

public sealed class DeleteImportSourceCommandHandler
    : IRequestHandler<DeleteImportSourceCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public DeleteImportSourceCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task Handle(
        DeleteImportSourceCommand request, CancellationToken cancellationToken)
    {
        var source = await _db.ImportSources
            .FirstOrDefaultAsync(s => s.Id == request.ImportSourceId, cancellationToken)
            ?? throw new NotFoundException("Import source not found.");
        await _access.RequireManageAsync(source.WorkspaceId, request.UserId, cancellationToken);
        _db.ImportSources.Remove(source);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
