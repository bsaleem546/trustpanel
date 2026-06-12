using System.Text.Json;
using TrustPanel.Domain.Common;

namespace TrustPanel.Application.Common.Behaviors;

/// <summary>
/// Stages immutable audit log rows into the current unit of work so they commit
/// atomically with the write command that caused them.
/// </summary>
public interface IAuditTrail
{
    void Record(
        Guid workspaceId,
        Guid? actorUserId,
        string action,
        string entityType,
        Guid? entityId,
        object? metadata = null);
}

public sealed class AuditTrail : IAuditTrail
{
    private readonly IAppDbContext _db;

    public AuditTrail(IAppDbContext db)
    {
        _db = db;
    }

    public void Record(
        Guid workspaceId,
        Guid? actorUserId,
        string action,
        string entityType,
        Guid? entityId,
        object? metadata = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            WorkspaceId = workspaceId,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Metadata = metadata is null ? "{}" : JsonSerializer.Serialize(metadata)
        });
    }
}
