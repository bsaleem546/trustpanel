namespace TrustPanel.Application.Workspaces;

public interface IDnsResolver
{
    /// <summary>Returns the CNAME targets for a host, without trailing dots.</summary>
    Task<IReadOnlyList<string>> GetCnameRecordsAsync(string host, CancellationToken cancellationToken);
}

/// <summary>The CNAME target customers must point their custom domain at.</summary>
public sealed record CustomDomainOptions(string CnameTarget);
