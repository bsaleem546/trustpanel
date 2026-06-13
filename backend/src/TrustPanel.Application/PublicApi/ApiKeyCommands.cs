using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Integrations;

namespace TrustPanel.Application.PublicApi;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record ApiKeyDto(
    Guid Id, string Name, string Prefix, bool IsActive,
    DateTimeOffset? LastUsedAt, DateTimeOffset CreatedAt);

// ── List ──────────────────────────────────────────────────────────────────────

public sealed record ListApiKeysQuery(Guid UserId, Guid WorkspaceId)
    : IRequest<IReadOnlyList<ApiKeyDto>>;

public sealed class ListApiKeysQueryHandler : IRequestHandler<ListApiKeysQuery, IReadOnlyList<ApiKeyDto>>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public ListApiKeysQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<IReadOnlyList<ApiKeyDto>> Handle(
        ListApiKeysQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);
        return await _db.ApiKeys
            .Where(k => k.WorkspaceId == request.WorkspaceId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyDto(k.Id, k.Name, k.Prefix, k.IsActive, k.LastUsedAt, k.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

// ── Create ────────────────────────────────────────────────────────────────────

public sealed record CreateApiKeyCommand(Guid UserId, Guid WorkspaceId, string Name)
    : IRequest<CreateApiKeyResult>;

public sealed record CreateApiKeyResult(ApiKeyDto Key, string PlaintextKey);

public sealed class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateApiKeyCommandHandler : IRequestHandler<CreateApiKeyCommand, CreateApiKeyResult>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public CreateApiKeyCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<CreateApiKeyResult> Handle(
        CreateApiKeyCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var plaintext = "tp_live_" + Convert.ToBase64String(rawBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        var keyHash = Convert.ToHexString(hash).ToLowerInvariant();
        var prefix = plaintext[..16];

        var key = new ApiKey
        {
            WorkspaceId = request.WorkspaceId,
            Name = request.Name,
            KeyHash = keyHash,
            Prefix = prefix
        };
        _db.ApiKeys.Add(key);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateApiKeyResult(
            new ApiKeyDto(key.Id, key.Name, key.Prefix, true, null, key.CreatedAt),
            plaintext);
    }

    public static string HashKey(string plaintext)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

// ── Rename ────────────────────────────────────────────────────────────────────

public sealed record RenameApiKeyCommand(Guid UserId, Guid WorkspaceId, Guid KeyId, string Name)
    : IRequest;

public sealed class RenameApiKeyCommandHandler : IRequestHandler<RenameApiKeyCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public RenameApiKeyCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task Handle(RenameApiKeyCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);
        var key = await _db.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.KeyId
                                   && k.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("API key not found.");
        key.Name = request.Name;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

// ── Revoke ────────────────────────────────────────────────────────────────────

public sealed record RevokeApiKeyCommand(Guid UserId, Guid WorkspaceId, Guid KeyId) : IRequest;

public sealed class RevokeApiKeyCommandHandler : IRequestHandler<RevokeApiKeyCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public RevokeApiKeyCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);
        var key = await _db.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.KeyId
                                   && k.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("API key not found.");
        key.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
