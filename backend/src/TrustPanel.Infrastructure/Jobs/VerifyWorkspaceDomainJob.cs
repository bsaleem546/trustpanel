using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;

namespace TrustPanel.Infrastructure.Jobs;

/// <summary>
/// Recurring job that re-checks every workspace custom domain CNAME and
/// transitions DomainVerifiedAt in both directions.
/// </summary>
public sealed class VerifyWorkspaceDomainJob
{
    private readonly IAppDbContext _db;
    private readonly DomainVerificationService _verification;
    private readonly ILogger<VerifyWorkspaceDomainJob> _logger;

    public VerifyWorkspaceDomainJob(
        IAppDbContext db,
        DomainVerificationService verification,
        ILogger<VerifyWorkspaceDomainJob> logger)
    {
        _db = db;
        _verification = verification;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var workspaces = await _db.Workspaces
            .Where(w => w.CustomDomain != null)
            .ToListAsync(cancellationToken);

        foreach (var workspace in workspaces)
        {
            var verified = await _verification.VerifyAsync(workspace, cancellationToken);
            _logger.LogInformation(
                "Domain verification for workspace {WorkspaceId} ({Domain}): {Verified}",
                workspace.Id, workspace.CustomDomain, verified);
        }
    }
}
