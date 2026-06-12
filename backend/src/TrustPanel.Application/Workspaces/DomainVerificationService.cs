using TrustPanel.Application.Common;
using TrustPanel.Domain.Workspaces;

namespace TrustPanel.Application.Workspaces;

/// <summary>
/// Checks a workspace's custom domain CNAME and transitions DomainVerifiedAt
/// in both directions (sets it when DNS is correct, clears it when it no longer is).
/// </summary>
public sealed class DomainVerificationService
{
    private readonly IDnsResolver _dnsResolver;
    private readonly CustomDomainOptions _options;
    private readonly IAppDbContext _db;

    public DomainVerificationService(
        IDnsResolver dnsResolver, CustomDomainOptions options, IAppDbContext db)
    {
        _dnsResolver = dnsResolver;
        _options = options;
        _db = db;
    }

    public async Task<bool> VerifyAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspace.CustomDomain))
        {
            return false;
        }

        bool pointsAtUs;
        try
        {
            var cnames = await _dnsResolver.GetCnameRecordsAsync(
                workspace.CustomDomain, cancellationToken);
            pointsAtUs = cnames.Any(c =>
                string.Equals(c.TrimEnd('.'), _options.CnameTarget, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // DNS resolution failures are treated as "not yet verified", never as a crash.
            pointsAtUs = false;
        }

        if (pointsAtUs && workspace.DomainVerifiedAt is null)
        {
            workspace.DomainVerifiedAt = DateTimeOffset.UtcNow;
            workspace.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (!pointsAtUs && workspace.DomainVerifiedAt is not null)
        {
            workspace.DomainVerifiedAt = null;
            workspace.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return pointsAtUs;
    }
}
