namespace TrustPanel.Application.Ai;

public sealed record SentimentResult(double Score, string? Highlight);
public sealed record InsightsReport(string Summary, IReadOnlyList<string> TopThemes, IReadOnlyList<string> Recommendations);

/// <summary>
/// AI capabilities backed by Anthropic. Implementations must only be invoked from
/// background jobs, never from synchronous request handlers.
/// </summary>
public interface IAiService
{
    /// <summary>Scores sentiment in [-1, 1] and extracts a short highlight for long text. Null when AI is unavailable.</summary>
    Task<SentimentResult?> AnalyzeSentimentAsync(string content, CancellationToken cancellationToken);

    /// <summary>Generates a suggested reply to a testimonial. Null when AI is unavailable.</summary>
    Task<string?> SuggestReplyAsync(string testimonialContent, string workspaceName, CancellationToken cancellationToken);

    /// <summary>Generates a workspace insights report from approved testimonial content. Null when AI is unavailable.</summary>
    Task<InsightsReport?> GenerateInsightsAsync(IReadOnlyList<string> recentApprovedContent, string workspaceName, CancellationToken cancellationToken);

    /// <summary>Filters imported testimonials, returning indices of entries that seem genuine. Null skips filtering.</summary>
    Task<IReadOnlyList<int>?> FilterImportedTestimonialsAsync(IReadOnlyList<string> contents, CancellationToken cancellationToken);
}

/// <summary>Fallback when no AI provider is configured: all operations are skipped.</summary>
public sealed class NullAiService : IAiService
{
    public Task<SentimentResult?> AnalyzeSentimentAsync(string content, CancellationToken cancellationToken)
        => Task.FromResult<SentimentResult?>(null);

    public Task<string?> SuggestReplyAsync(string testimonialContent, string workspaceName, CancellationToken cancellationToken)
        => Task.FromResult<string?>(null);

    public Task<InsightsReport?> GenerateInsightsAsync(IReadOnlyList<string> recentApprovedContent, string workspaceName, CancellationToken cancellationToken)
        => Task.FromResult<InsightsReport?>(null);

    public Task<IReadOnlyList<int>?> FilterImportedTestimonialsAsync(IReadOnlyList<string> contents, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<int>?>(null);
}
