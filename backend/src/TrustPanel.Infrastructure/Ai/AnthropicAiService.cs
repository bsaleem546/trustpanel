using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TrustPanel.Application.Ai;

namespace TrustPanel.Infrastructure.Ai;

/// <summary>
/// Anthropic Messages API implementation. All methods gracefully degrade to null on failure.
/// Must only be called from Hangfire jobs.
/// </summary>
public sealed class AnthropicAiService : IAiService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _insightsModel;
    private readonly ILogger<AnthropicAiService> _logger;

    public AnthropicAiService(HttpClient http, string model, string insightsModel, ILogger<AnthropicAiService> logger)
    {
        _http = http;
        _model = model;
        _insightsModel = insightsModel;
        _logger = logger;
    }

    public async Task<SentimentResult?> AnalyzeSentimentAsync(string content, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $$"""
                Analyze the sentiment of this customer testimonial and extract a short highlight (max 25 words).

                Testimonial: {{content}}

                Respond ONLY with a JSON object: {"score": <float -1.0 to 1.0>, "highlight": "<short highlight or null>"}
                """;

            var json = await SendAsync(_model, prompt, 200, cancellationToken);
            if (json is null) return null;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var score = root.GetProperty("score").GetDouble();
            var highlight = root.TryGetProperty("highlight", out var h) && h.ValueKind != JsonValueKind.Null
                ? h.GetString()
                : null;
            return new SentimentResult(score, highlight);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AnthropicAiService.AnalyzeSentimentAsync failed.");
            return null;
        }
    }

    public async Task<string?> SuggestReplyAsync(string testimonialContent, string workspaceName, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $"""
                Write a warm, professional reply to this customer testimonial for {workspaceName}.
                Keep it under 3 sentences, personalize it, and end with a thank-you.

                Testimonial: {testimonialContent}

                Reply only with the reply text, no quotes or extra formatting.
                """;

            return await SendAsync(_model, prompt, 150, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AnthropicAiService.SuggestReplyAsync failed.");
            return null;
        }
    }

    public async Task<InsightsReport?> GenerateInsightsAsync(
        IReadOnlyList<string> recentApprovedContent, string workspaceName, CancellationToken cancellationToken)
    {
        try
        {
            if (recentApprovedContent.Count == 0) return null;

            var snippets = string.Join("\n---\n", recentApprovedContent.Take(50));
            var prompt = $$"""
                Analyze these recent approved testimonials for {{workspaceName}} and produce a business insights report.

                Testimonials:
                {{snippets}}

                Respond ONLY with JSON: {"summary": "<2-3 sentence summary>", "topThemes": ["<theme>", ...], "recommendations": ["<rec>", ...]}
                """;

            var json = await SendAsync(_insightsModel, prompt, 600, cancellationToken);
            if (json is null) return null;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var summary = root.GetProperty("summary").GetString() ?? string.Empty;
            var themes = root.GetProperty("topThemes").EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty).ToList();
            var recs = root.GetProperty("recommendations").EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty).ToList();
            return new InsightsReport(summary, themes, recs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AnthropicAiService.GenerateInsightsAsync failed.");
            return null;
        }
    }

    public async Task<IReadOnlyList<int>?> FilterImportedTestimonialsAsync(
        IReadOnlyList<string> contents, CancellationToken cancellationToken)
    {
        try
        {
            if (contents.Count == 0) return [];

            var numbered = string.Join("\n", contents.Select((c, i) => $"{i}: {c}"));
            var prompt = $$"""
                Review these imported reviews. Return the indices of reviews that appear genuine (not spam, not offensive).

                Reviews:
                {{numbered}}

                Respond ONLY with JSON: {"indices": [<int>, ...]}
                """;

            var json = await SendAsync(_model, prompt, 200, cancellationToken);
            if (json is null) return null;

            using var doc = JsonDocument.Parse(json);
            var indices = doc.RootElement.GetProperty("indices").EnumerateArray()
                .Select(e => e.GetInt32()).ToList();
            return indices;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AnthropicAiService.FilterImportedTestimonialsAsync failed.");
            return null;
        }
    }

    private async Task<string?> SendAsync(string model, string userPrompt, int maxTokens, CancellationToken cancellationToken)
    {
        var request = new
        {
            model,
            max_tokens = maxTokens,
            messages = new[] { new { role = "user", content = userPrompt } }
        };

        using var response = await _http.PostAsJsonAsync("https://api.anthropic.com/v1/messages", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Anthropic API returned {StatusCode}.", response.StatusCode);
            return null;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
    }
}
