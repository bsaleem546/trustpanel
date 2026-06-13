using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TrustPanel.Application.Ai;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Testimonials;
using TrustPanel.Infrastructure.Jobs;

namespace TrustPanel.IntegrationTests;

public sealed class AiTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public AiTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Sentiment_job_persists_score_and_auto_approves()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "ai-sentiment@example.com");

        Guid testimonialId = default;
        await _factory.InDbAsync(async db =>
        {
            var testimonial = new Testimonial
            {
                WorkspaceId = user.WorkspaceId,
                Status = TestimonialStatus.Pending,
                Type = TestimonialType.Text,
                Content = "Absolutely amazing product! Exceeded all expectations.",
                Source = TestimonialSource.Form,
                Rating = 5,
                Submitter = new TestimonialSubmitter { Name = "Bob" }
            };
            db.Testimonials.Add(testimonial);
            await db.SaveChangesAsync();
            testimonialId = testimonial.Id;
        });

        // Replace AI service with fake that returns high sentiment.
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAiService));
                if (descriptor is not null) services.Remove(descriptor);
                services.AddScoped<IAiService>(_ => new FakeHighSentimentAiService());
            });
        });

        using var scope = factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<AnalyzeTestimonialSentimentJob>();
        await job.RunAsync(testimonialId, CancellationToken.None);

        await _factory.InDbAsync(async db =>
        {
            var t = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(db.Testimonials, t => t.Id == testimonialId);
            t!.SentimentScore.Should().BeGreaterThan(0.4);
            t.Status.Should().Be(TestimonialStatus.Approved, "auto-approve rule: rating>=4 and sentiment>0.4");
        });
    }

    [Fact]
    public async Task Insights_job_caches_report()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "ai-insights2@example.com");

        using var scope = _factory.Services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
        var cacheKey = GenerateWorkspaceInsightsJob.CacheKey(user.WorkspaceId);

        // Pre-populate cache to simulate job completion.
        var report = new InsightsReport("Great feedback.", ["quality"], ["improve response time"]);
        var json = System.Text.Json.JsonSerializer.Serialize(report);
        await cache.SetAsync(cacheKey, json, GenerateWorkspaceInsightsJob.CacheTtl, CancellationToken.None);

        var res = await client.GetAsync($"/api/ai/insights?workspaceId={user.WorkspaceId}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        data.TryGetProperty("summary", out _).Should().BeTrue("cached insights should be returned");
    }

    [Fact]
    public async Task Reply_suggestion_endpoint_returns_ok()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "ai-reply@example.com");

        Guid testimonialId = default;
        await _factory.InDbAsync(async db =>
        {
            var t = new Testimonial
            {
                WorkspaceId = user.WorkspaceId,
                Status = TestimonialStatus.Approved,
                Type = TestimonialType.Text,
                Content = "Love this product.",
                Source = TestimonialSource.Form,
                Submitter = new TestimonialSubmitter { Name = "Dave" }
            };
            db.Testimonials.Add(t);
            await db.SaveChangesAsync();
            testimonialId = t.Id;
        });

        var res = await client.GetAsync(
            $"/api/ai/reply-suggestion/{testimonialId}?workspaceId={user.WorkspaceId}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        // Either "generating: true" or "suggestion: <text>"
        (data.TryGetProperty("generating", out _) || data.TryGetProperty("suggestion", out _))
            .Should().BeTrue();
    }

    [Fact]
    public async Task NullAiService_returns_null_for_all_methods()
    {
        var svc = new NullAiService();
        var ct = CancellationToken.None;

        (await svc.AnalyzeSentimentAsync("test", ct)).Should().BeNull();
        (await svc.SuggestReplyAsync("test", "workspace", ct)).Should().BeNull();
        (await svc.GenerateInsightsAsync(["test"], "workspace", ct)).Should().BeNull();
        (await svc.FilterImportedTestimonialsAsync(["test"], ct)).Should().BeNull();
    }

    private sealed class FakeHighSentimentAiService : IAiService
    {
        public Task<SentimentResult?> AnalyzeSentimentAsync(string content, CancellationToken cancellationToken)
            => Task.FromResult<SentimentResult?>(new SentimentResult(0.9, "Amazing product"));

        public Task<string?> SuggestReplyAsync(string testimonialContent, string workspaceName, CancellationToken cancellationToken)
            => Task.FromResult<string?>("Thank you for your kind words!");

        public Task<InsightsReport?> GenerateInsightsAsync(IReadOnlyList<string> recentApprovedContent, string workspaceName, CancellationToken cancellationToken)
            => Task.FromResult<InsightsReport?>(new InsightsReport("Great.", ["quality"], ["keep it up"]));

        public Task<IReadOnlyList<int>?> FilterImportedTestimonialsAsync(IReadOnlyList<string> contents, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<int>?>(Enumerable.Range(0, contents.Count).ToList());
    }
}
