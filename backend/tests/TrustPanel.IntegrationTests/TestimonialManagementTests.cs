using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrustPanel.Domain.Testimonials;
using TrustPanel.Infrastructure.Jobs;
using TrustPanel.Infrastructure.Persistence;

namespace TrustPanel.IntegrationTests;

public sealed class TestimonialManagementTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public TestimonialManagementTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(TestHelpers.TestUser User, Guid TestimonialId)> SeedTestimonialAsync(
        HttpClient client, string email, TestimonialStatus status = TestimonialStatus.Pending)
    {
        var user = await _factory.CreateUserAsync(client, email);

        var testimonial = new Testimonial
        {
            WorkspaceId = user.WorkspaceId,
            Status = status,
            Type = TestimonialType.Text,
            Content = "Great product!",
            Source = TestimonialSource.Form,
            Rating = 5,
            Submitter = new TestimonialSubmitter { Name = "Alice", Email = "alice@example.com" }
        };

        await _factory.InDbAsync(db =>
        {
            db.Testimonials.Add(testimonial);
            return db.SaveChangesAsync();
        });

        return (user, testimonial.Id);
    }

    [Fact]
    public async Task Approve_transitions_status_to_approved_and_writes_audit_row()
    {
        var client = _factory.CreateHttpsClient();
        var (user, testimonialId) = await SeedTestimonialAsync(client, "approve-test@example.com");

        var res = await client.PostAsync($"/api/testimonials/{testimonialId}/approve", null);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        data.GetProperty("status").GetInt32().Should().Be((int)TestimonialStatus.Approved);

        var auditExists = await _factory.InDbAsync(db =>
            db.AuditLogs.AnyAsync(a => a.EntityId == testimonialId && a.Action == "testimonial.approved"));
        auditExists.Should().BeTrue();
    }

    [Fact]
    public async Task Reject_transitions_status_to_rejected()
    {
        var client = _factory.CreateHttpsClient();
        var (user, testimonialId) = await SeedTestimonialAsync(client, "reject-test@example.com");

        var res = await client.PostAsync($"/api/testimonials/{testimonialId}/reject", null);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        data.GetProperty("status").GetInt32().Should().Be((int)TestimonialStatus.Rejected);
    }

    [Fact]
    public async Task Feature_sets_featuredAt_and_unfeature_clears_it()
    {
        var client = _factory.CreateHttpsClient();
        var (user, testimonialId) = await SeedTestimonialAsync(
            client, "feature-test@example.com", TestimonialStatus.Approved);

        var feature = await client.PostAsync($"/api/testimonials/{testimonialId}/feature?featured=true", null);
        feature.StatusCode.Should().Be(HttpStatusCode.OK);
        var featured = await feature.ReadDataAsync();
        featured.GetProperty("featuredAt").ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Null);

        var unfeature = await client.PostAsync($"/api/testimonials/{testimonialId}/feature?featured=false", null);
        unfeature.StatusCode.Should().Be(HttpStatusCode.OK);
        var unfeatured = await unfeature.ReadDataAsync();
        unfeatured.GetProperty("featuredAt").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task Edit_updates_content_and_sets_editedAt()
    {
        var client = _factory.CreateHttpsClient();
        var (user, testimonialId) = await SeedTestimonialAsync(client, "edit-test@example.com");

        var res = await client.PutAsJsonAsync($"/api/testimonials/{testimonialId}",
            new { content = "Updated content", rating = 4 });
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        data.GetProperty("content").GetString().Should().Be("Updated content");
        data.GetProperty("rating").GetInt32().Should().Be(4);
        data.GetProperty("editedAt").ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Null);

        var auditExists = await _factory.InDbAsync(db =>
            db.AuditLogs.AnyAsync(a => a.EntityId == testimonialId && a.Action == "testimonial.edited"));
        auditExists.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_removes_testimonial_and_writes_audit_row()
    {
        var client = _factory.CreateHttpsClient();
        var (user, testimonialId) = await SeedTestimonialAsync(client, "delete-test@example.com");

        var res = await client.DeleteAsync($"/api/testimonials/{testimonialId}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var exists = await _factory.InDbAsync(db =>
            db.Testimonials.AnyAsync(t => t.Id == testimonialId));
        exists.Should().BeFalse();

        // Audit row was written before removal.
        var auditExists = await _factory.InDbAsync(db =>
            db.AuditLogs.AnyAsync(a => a.EntityId == testimonialId && a.Action == "testimonial.deleted"));
        auditExists.Should().BeTrue();
    }

    [Fact]
    public async Task Batch_approve_updates_all_testimonials()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "batch-test@example.com");

        var ids = new List<Guid>();
        await _factory.InDbAsync(async db =>
        {
            for (var i = 0; i < 3; i++)
            {
                var t = new Testimonial
                {
                    WorkspaceId = user.WorkspaceId,
                    Status = TestimonialStatus.Pending,
                    Type = TestimonialType.Text,
                    Content = $"Batch testimonial {i}",
                    Source = TestimonialSource.Form,
                    Submitter = new TestimonialSubmitter { Name = $"User {i}" }
                };
                db.Testimonials.Add(t);
                ids.Add(t.Id);
            }
            await db.SaveChangesAsync();
        });

        var res = await client.PostAsJsonAsync("/api/testimonials/batch", new
        {
            workspaceId = user.WorkspaceId,
            testimonialIds = ids,
            action = (int)TrustPanel.Application.Testimonials.BatchTestimonialAction.Approve
        });
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        data.GetProperty("affected").GetInt32().Should().Be(3);

        var allApproved = await _factory.InDbAsync(db =>
            db.Testimonials.Where(t => ids.Contains(t.Id))
                .AllAsync(t => t.Status == TestimonialStatus.Approved));
        allApproved.Should().BeTrue();
    }

    [Fact]
    public async Task List_filters_by_status_and_paginates()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "list-filter@example.com");

        await _factory.InDbAsync(async db =>
        {
            db.Testimonials.Add(new Testimonial
            {
                WorkspaceId = user.WorkspaceId, Status = TestimonialStatus.Approved,
                Type = TestimonialType.Text, Content = "Approved one",
                Source = TestimonialSource.Form,
                Submitter = new TestimonialSubmitter { Name = "Alice" }
            });
            db.Testimonials.Add(new Testimonial
            {
                WorkspaceId = user.WorkspaceId, Status = TestimonialStatus.Pending,
                Type = TestimonialType.Text, Content = "Pending one",
                Source = TestimonialSource.Form,
                Submitter = new TestimonialSubmitter { Name = "Bob" }
            });
            await db.SaveChangesAsync();
        });

        var res = await client.GetAsync(
            $"/api/testimonials?workspaceId={user.WorkspaceId}&status=Approved&page=1&pageSize=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        var items = data.GetProperty("items");
        items.GetArrayLength().Should().BeGreaterThan(0);
        foreach (var item in items.EnumerateArray())
        {
            item.GetProperty("status").GetInt32().Should().Be((int)TestimonialStatus.Approved);
        }
    }

    [Fact]
    public async Task Search_falls_back_to_sql_when_indexer_returns_null()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "search-fallback@example.com");

        await _factory.InDbAsync(async db =>
        {
            db.Testimonials.Add(new Testimonial
            {
                WorkspaceId = user.WorkspaceId, Status = TestimonialStatus.Approved,
                Type = TestimonialType.Text, Content = "Amazing widget",
                Source = TestimonialSource.Form,
                Submitter = new TestimonialSubmitter { Name = "Tester" }
            });
            await db.SaveChangesAsync();
        });

        // Factory's FakeSearchIndexer.SearchResults is null → SQL fallback.
        _factory.SearchIndexer.SearchResults = null;

        var res = await client.GetAsync(
            $"/api/testimonials/search?workspaceId={user.WorkspaceId}&q=amazing&limit=10");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        data.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Csv_import_job_creates_testimonials()
    {
        var user = await _factory.InDbAsync(async db =>
        {
            // We need a workspace; reuse one created in an earlier test or create directly.
            var workspace = await db.Workspaces.FirstOrDefaultAsync();
            return workspace?.Id;
        });

        // Create an import source then run the CSV import job directly.
        Guid importSourceId = Guid.Empty;
        if (user.HasValue)
        {
            await _factory.InDbAsync(async db =>
            {
                var src = new TrustPanel.Domain.Integrations.ImportSource
                {
                    WorkspaceId = user.Value,
                    Provider = TrustPanel.Domain.Integrations.ImportProvider.Csv
                };
                db.ImportSources.Add(src);
                await db.SaveChangesAsync();
                importSourceId = src.Id;
            });

            using var scope = _factory.Services.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<ImportTestimonialsCsvJob>();
            var csv = "Name,Email,Content,Rating\nJohn,john@test.com,Loved it,5\nJane,jane@test.com,Very helpful,4";
            await job.ExecuteAsync(importSourceId, csv);

            var count = await _factory.InDbAsync(db =>
                db.Testimonials.CountAsync(t =>
                    t.WorkspaceId == user.Value && t.Source == TestimonialSource.Csv));
            count.Should().Be(2);
        }
    }
}
