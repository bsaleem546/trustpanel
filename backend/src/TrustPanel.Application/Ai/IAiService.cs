namespace TrustPanel.Application.Ai;

public sealed record SentimentResult(double Score, string? Highlight);

/// <summary>
/// AI capabilities backed by Anthropic. Implementations must only be invoked from
/// background jobs, never from synchronous request handlers.
/// </summary>
public interface IAiService
{
    /// <summary>Scores sentiment in [-1, 1] and extracts a short highlight for long text. Null when AI is unavailable.</summary>
    Task<SentimentResult?> AnalyzeSentimentAsync(string content, CancellationToken cancellationToken);
}

/// <summary>Fallback when no AI provider is configured: analysis is skipped.</summary>
public sealed class NullAiService : IAiService
{
    public Task<SentimentResult?> AnalyzeSentimentAsync(string content, CancellationToken cancellationToken)
        => Task.FromResult<SentimentResult?>(null);
}
