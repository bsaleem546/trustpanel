using DnsClient;
using TrustPanel.Application.Workspaces;

namespace TrustPanel.Infrastructure.Security;

public sealed class DnsClientResolver : IDnsResolver
{
    private readonly LookupClient _client = new();

    public async Task<IReadOnlyList<string>> GetCnameRecordsAsync(
        string host, CancellationToken cancellationToken)
    {
        var result = await _client.QueryAsync(
            host, QueryType.CNAME, cancellationToken: cancellationToken);
        return result.Answers.CnameRecords()
            .Select(record => record.CanonicalName.Value.TrimEnd('.'))
            .ToList();
    }
}
