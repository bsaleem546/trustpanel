using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrustPanel.Application.Ai;
using TrustPanel.Domain.Testimonials;
using TrustPanel.Infrastructure.Jobs;
using TrustPanel.Infrastructure.Persistence;

namespace TrustPanel.IntegrationTests;

public sealed class FormSubmissionTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public FormSubmissionTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(TestHelpers.TestUser User, string WorkspaceSlug, string FormSlug, Guid FormId)>
        CreateUserWithFormAsync(HttpClient client, string email, object? formOverrides = null)
    {
        var user = await _factory.CreateUserAsync(client, email);
        var create = await client.PostAsJsonAsync("/api/forms",
            formOverrides ?? new { name = "Main form" });
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        var form = await create.ReadDataAsync();
        var formSlug = form.GetProperty("slug").GetString()!;
        var formId = form.GetProperty("id").GetGuid();

        var workspaceSlug = await _factory.InDbAsync(db => db.Workspaces
            .Where(w => w.Id == user.WorkspaceId)
            .Select(w => w.Slug)
            .SingleAsync());

        return (user, workspaceSlug, formSlug, formId);
    }

    [Fact]
    public async Task Form_builder_crud_round_trips_configuration()
    {
        var client = _factory.CreateHttpsClient();
        await _factory.CreateUserAsync(client, "form-crud@example.com");

        var create = await client.PostAsJsonAsync("/api/forms", new
        {
            name = "Launch reviews",
            allowedSubmissionType = 0,
            questionConfig = new
            {
                welcomeTitle = "Tell us!",
                welcomeMessage = "We appreciate it",
                prompt = "How was it?",
                collectName = true,
                collectEmail = true,
                collectCompany = true,
                collectJobTitle = false,
                collectAvatar = false,
                collectRating = true,
                requireEmail = false
            },
            thankYouConfig = new
            {
                title = "Cheers!",
                message = "You rock",
                redirectUrl = "https://example.com/thanks",
                showSocialShare = true
            },
            rewardConfig = new
            {
                enabled = true,
                description = "10% off",
                couponCode = "THANKS10",
                rewardUrl = (string?)null
            }
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.ReadDataAsync();
        var formId = created.GetProperty("id").GetGuid();
        created.GetProperty("slug").GetString().Should().Be("launch-reviews");
        created.GetProperty("questionConfig").GetProperty("collectCompany").GetBoolean().Should().BeTrue();
        created.GetProperty("rewardConfig").GetProperty("couponCode").GetString().Should().Be("THANKS10");

        var update = await client.PutAsJsonAsync($"/api/forms/{formId}",
            new { name = "Launch reviews", isActive = false });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        (await update.ReadDataAsync()).GetProperty("isActive").GetBoolean().Should().BeFalse();

        var list = await client.GetAsync("/api/forms");
        (await list.ReadDataAsync()).GetProperty("total").GetInt32().Should().BeGreaterThan(0);

        var delete = await client.DeleteAsync($"/api/forms/{formId}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);
        var getAfter = await client.GetAsync($"/api/forms/{formId}");
        getAfter.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Public_form_is_readable_by_workspace_slug_and_custom_domain_host()
    {
        var client = _factory.CreateHttpsClient();
        var (user, workspaceSlug, formSlug, _) =
            await CreateUserWithFormAsync(client, "form-public@example.com");

        var anonymous = _factory.CreateHttpsClient();
        var bySlug = await anonymous.GetAsync($"/api/public/forms/{workspaceSlug}/{formSlug}");
        bySlug.StatusCode.Should().Be(HttpStatusCode.OK);
        var form = await bySlug.ReadDataAsync();
        form.GetProperty("formSlug").GetString().Should().Be(formSlug);
        form.GetProperty("questions").GetProperty("prompt").GetString().Should().NotBeNullOrEmpty();
        bySlug.Headers.TryGetValues("Content-Security-Policy", out var csp).Should().BeTrue();
        csp!.Single().Should().Contain("default-src");

        // Verified custom domain host resolves the workspace without a slug.
        const string domain = "reviews.form-host.com";
        await _factory.InDbAsync(async db =>
        {
            var workspace = await db.Workspaces.SingleAsync(w => w.Id == user.WorkspaceId);
            workspace.CustomDomain = domain;
            workspace.DomainVerifiedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        });
        using var hostRequest = new HttpRequestMessage(
            HttpMethod.Get, $"/api/public/forms/{formSlug}");
        hostRequest.Headers.Host = domain;
        var byHost = await anonymous.SendAsync(hostRequest);
        byHost.StatusCode.Should().Be(HttpStatusCode.OK);
        (await byHost.ReadDataAsync()).GetProperty("workspaceId").GetGuid().Should().Be(user.WorkspaceId);

        var missing = await anonymous.GetAsync($"/api/public/forms/{workspaceSlug}/nope");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Valid_submission_creates_pending_testimonial_and_dispatches_jobs()
    {
        var client = _factory.CreateHttpsClient();
        var (user, workspaceSlug, formSlug, formId) =
            await CreateUserWithFormAsync(client, "form-submit@example.com");
        _factory.Jobs.Enqueued.Clear();

        var anonymous = _factory.CreateHttpsClient();
        var response = await anonymous.PostAsJsonAsync(
            $"/api/public/forms/{workspaceSlug}/{formSlug}/submissions",
            new
            {
                content = "Absolutely loved working with this team!",
                rating = 5,
                name = "Jane Doe",
                email = "jane@example.com",
                turnstileToken = "ok-token"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var data = await response.ReadDataAsync();
        var testimonialId = data.GetProperty("testimonialId").GetGuid();
        data.GetProperty("thankYou").GetProperty("title").GetString().Should().NotBeNullOrEmpty();

        var testimonial = await _factory.InDbAsync(db => db.Testimonials
            .SingleAsync(t => t.Id == testimonialId));
        testimonial.Status.Should().Be(TestimonialStatus.Pending);
        testimonial.WorkspaceId.Should().Be(user.WorkspaceId);
        testimonial.CollectionFormId.Should().Be(formId);
        testimonial.Submitter.Name.Should().Be("Jane Doe");

        // Trial users run on Agency+ (AI enabled): all three jobs dispatch.
        var jobTypes = _factory.Jobs.Enqueued.Select(j => j.JobType).ToList();
        jobTypes.Should().Contain(typeof(SendTestimonialThankYouJob));
        jobTypes.Should().Contain(typeof(NotifyWorkspaceOwnerJob));
        jobTypes.Should().Contain(typeof(AnalyzeTestimonialSentimentJob));
    }

    [Fact]
    public async Task Turnstile_failure_returns_validation_envelope()
    {
        var client = _factory.CreateHttpsClient();
        var (_, workspaceSlug, formSlug, _) =
            await CreateUserWithFormAsync(client, "form-turnstile@example.com");
        _factory.Turnstile.FailingTokens.Add("bot-token");

        var anonymous = _factory.CreateHttpsClient();
        var response = await anonymous.PostAsJsonAsync(
            $"/api/public/forms/{workspaceSlug}/{formSlug}/submissions",
            new
            {
                content = "Suspicious content",
                name = "Bot",
                email = "bot@example.com",
                turnstileToken = "bot-token"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await response.ReadEnvelopeAsync();
        envelope.GetProperty("status").GetBoolean().Should().BeFalse();
        envelope.GetProperty("errors").TryGetProperty("turnstileToken", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Sixth_submission_from_same_ip_is_rate_limited()
    {
        var client = _factory.CreateHttpsClient();
        var (_, workspaceSlug, formSlug, _) =
            await CreateUserWithFormAsync(client, "form-ratelimit@example.com");

        var anonymous = _factory.CreateHttpsClient();
        anonymous.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.7");
        for (var i = 1; i <= 5; i++)
        {
            var ok = await anonymous.PostAsJsonAsync(
                $"/api/public/forms/{workspaceSlug}/{formSlug}/submissions",
                new { content = $"Submission {i}", name = $"Visitor {i}", email = "v@example.com" });
            ok.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var blocked = await anonymous.PostAsJsonAsync(
            $"/api/public/forms/{workspaceSlug}/{formSlug}/submissions",
            new { content = "One too many", name = "Visitor 6", email = "v@example.com" });

        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var envelope = await blocked.ReadEnvelopeAsync();
        envelope.GetProperty("code").GetInt32().Should().Be(429);
        envelope.GetProperty("error").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Inactive_form_and_missing_required_email_are_rejected()
    {
        var client = _factory.CreateHttpsClient();
        var (_, workspaceSlug, formSlug, formId) =
            await CreateUserWithFormAsync(client, "form-rules@example.com");

        // RequireEmail defaults to true.
        var anonymous = _factory.CreateHttpsClient();
        var noEmail = await anonymous.PostAsJsonAsync(
            $"/api/public/forms/{workspaceSlug}/{formSlug}/submissions",
            new { content = "No email supplied", name = "Anon" });
        noEmail.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await noEmail.ReadEnvelopeAsync()).GetProperty("errors")
            .TryGetProperty("email", out _).Should().BeTrue();

        // Deactivated forms vanish from the public surface.
        var deactivate = await client.PutAsJsonAsync($"/api/forms/{formId}",
            new { name = "Main form", isActive = false });
        deactivate.StatusCode.Should().Be(HttpStatusCode.OK);
        var read = await anonymous.GetAsync($"/api/public/forms/{workspaceSlug}/{formSlug}");
        read.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Sentiment_job_scores_and_auto_approves_positive_high_rating_testimonials()
    {
        var client = _factory.CreateHttpsClient();
        var (user, workspaceSlug, formSlug, _) =
            await CreateUserWithFormAsync(client, "form-sentiment@example.com");

        var anonymous = _factory.CreateHttpsClient();
        var submit = await anonymous.PostAsJsonAsync(
            $"/api/public/forms/{workspaceSlug}/{formSlug}/submissions",
            new { content = "Fantastic!", rating = 5, name = "Happy", email = "h@example.com" });
        var testimonialId = (await submit.ReadDataAsync()).GetProperty("testimonialId").GetGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var job = new AnalyzeTestimonialSentimentJob(
                db,
                new FakeAiService(new SentimentResult(0.9, "Fantastic")),
                scope.ServiceProvider
                    .GetRequiredService<Microsoft.Extensions.Logging.ILogger<AnalyzeTestimonialSentimentJob>>());
            await job.RunAsync(testimonialId, CancellationToken.None);
        }

        var testimonial = await _factory.InDbAsync(db => db.Testimonials
            .SingleAsync(t => t.Id == testimonialId));
        testimonial.SentimentScore.Should().Be(0.9);
        testimonial.Highlight.Should().Be("Fantastic");
        testimonial.Status.Should().Be(TestimonialStatus.Approved);

        // Low sentiment stays pending.
        var submit2 = await anonymous.PostAsJsonAsync(
            $"/api/public/forms/{workspaceSlug}/{formSlug}/submissions",
            new { content = "It was fine I guess", rating = 5, name = "Meh", email = "m@example.com" });
        var pendingId = (await submit2.ReadDataAsync()).GetProperty("testimonialId").GetGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var job = new AnalyzeTestimonialSentimentJob(
                db,
                new FakeAiService(new SentimentResult(0.1, null)),
                scope.ServiceProvider
                    .GetRequiredService<Microsoft.Extensions.Logging.ILogger<AnalyzeTestimonialSentimentJob>>());
            await job.RunAsync(pendingId, CancellationToken.None);
        }

        var pending = await _factory.InDbAsync(db => db.Testimonials
            .SingleAsync(t => t.Id == pendingId));
        pending.Status.Should().Be(TestimonialStatus.Pending);
        pending.SentimentScore.Should().Be(0.1);
    }

    private sealed class FakeAiService : IAiService
    {
        private readonly SentimentResult? _result;

        public FakeAiService(SentimentResult? result)
        {
            _result = result;
        }

        public Task<SentimentResult?> AnalyzeSentimentAsync(
            string content, CancellationToken cancellationToken)
            => Task.FromResult(_result);
    }
}
