namespace TrustPanel.Application.Common;

public interface ICurrentWorkspace
{
    Guid? WorkspaceId { get; }
}

/// <summary>
/// Scoped, mutable workspace context populated by workspace resolution middleware.
/// Register as both <see cref="WorkspaceContext"/> and <see cref="ICurrentWorkspace"/>.
/// </summary>
public sealed class WorkspaceContext : ICurrentWorkspace
{
    public Guid? WorkspaceId { get; set; }
}
