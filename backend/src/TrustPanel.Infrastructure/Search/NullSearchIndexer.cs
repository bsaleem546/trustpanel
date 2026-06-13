using TrustPanel.Application.Common;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Infrastructure.Search;

/// <summary>No-op search indexer used when Meilisearch is not configured.</summary>
public sealed class NullSearchIndexer : ISearchIndexer
{
    public Task IndexAsync(IReadOnlyList<Testimonial> testimonials, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task RemoveAsync(IReadOnlyList<Guid> testimonialIds, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<IReadOnlyList<Guid>?> SearchAsync(
        Guid workspaceId, string query, int limit, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Guid>?>(null);
}
