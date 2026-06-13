using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Infrastructure.Search;

/// <summary>
/// EF Core interceptor that keeps the search index in sync after SaveChanges.
/// Runs after the DB commit so index failures never roll back the write.
/// </summary>
public sealed class SearchIndexSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ISearchIndexer _indexer;

    public SearchIndexSaveChangesInterceptor(ISearchIndexer indexer)
    {
        _indexer = indexer;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return result;

        var added = eventData.Context.ChangeTracker.Entries<Testimonial>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .Select(e => e.Entity)
            .ToList();

        var deleted = eventData.Context.ChangeTracker.Entries<Testimonial>()
            .Where(e => e.State == EntityState.Deleted)
            .Select(e => e.Entity.Id)
            .ToList();

        if (added.Count > 0)
            await _indexer.IndexAsync(added, cancellationToken);

        if (deleted.Count > 0)
            await _indexer.RemoveAsync(deleted, cancellationToken);

        return result;
    }
}
