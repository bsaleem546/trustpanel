using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Application.Common;

/// <summary>
/// Full-text search backend (Meilisearch). Indexing is dispatched from an EF Core
/// SaveChanges interceptor; search falls back to SQL when the backend is unavailable.
/// </summary>
public interface ISearchIndexer
{
    Task IndexAsync(IReadOnlyList<Testimonial> testimonials, CancellationToken cancellationToken);
    Task RemoveAsync(IReadOnlyList<Guid> testimonialIds, CancellationToken cancellationToken);

    /// <summary>Matching testimonial IDs in relevance order, or null when the backend is unavailable.</summary>
    Task<IReadOnlyList<Guid>?> SearchAsync(
        Guid workspaceId, string query, int limit, CancellationToken cancellationToken);
}
