using Meilisearch;
using Microsoft.Extensions.Logging;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Infrastructure.Search;

/// <summary>
/// Meilisearch-backed testimonial full-text indexer.
/// Index name: "testimonials"; documents keyed by composite "workspaceId|id".
/// </summary>
public sealed class MeilisearchTestimonialIndexer : ISearchIndexer
{
    private const string IndexName = "testimonials";
    private readonly MeilisearchClient _client;
    private readonly ILogger<MeilisearchTestimonialIndexer> _logger;

    public MeilisearchTestimonialIndexer(
        MeilisearchClient client,
        ILogger<MeilisearchTestimonialIndexer> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task IndexAsync(
        IReadOnlyList<Testimonial> testimonials, CancellationToken cancellationToken)
    {
        if (testimonials.Count == 0) return;
        try
        {
            var docs = testimonials.Select(t => new
            {
                id = t.Id.ToString(),
                workspaceId = t.WorkspaceId.ToString(),
                content = t.Content,
                submitterName = t.Submitter.Name,
                tags = t.Tags,
                status = t.Status.ToString()
            }).ToList();

            var index = _client.Index(IndexName);
            await index.AddDocumentsAsync(docs, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Meilisearch indexing failed — search degraded");
        }
    }

    public async Task RemoveAsync(
        IReadOnlyList<Guid> testimonialIds, CancellationToken cancellationToken)
    {
        if (testimonialIds.Count == 0) return;
        try
        {
            var index = _client.Index(IndexName);
            await index.DeleteDocumentsAsync(
                testimonialIds.Select(id => id.ToString()).ToList(),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Meilisearch delete failed");
        }
    }

    public async Task<IReadOnlyList<Guid>?> SearchAsync(
        Guid workspaceId, string query, int limit, CancellationToken cancellationToken)
    {
        try
        {
            var index = _client.Index(IndexName);
            var result = await index.SearchAsync<MeilisearchDoc>(query, new SearchQuery
            {
                Filter = $"workspaceId = \"{workspaceId}\"",
                Limit = limit
            }, cancellationToken);

            return result.Hits
                .Select(h => Guid.TryParse(h.Id, out var g) ? (Guid?)g : null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Meilisearch search failed — falling back to SQL");
            return null;
        }
    }

    private sealed class MeilisearchDoc
    {
        public string Id { get; set; } = string.Empty;
        public string WorkspaceId { get; set; } = string.Empty;
    }
}
