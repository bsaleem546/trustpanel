using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrustPanel.Domain.Analytics;
using TrustPanel.Domain.Testimonials;
using TrustPanel.Domain.Widgets;
using TrustPanel.Infrastructure.Jobs;

namespace TrustPanel.IntegrationTests;

public sealed class AnalyticsTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public AnalyticsTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Event_ingestion_records_widget_view()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "analytics-event@example.com");

        Guid widgetId = default;
        await _factory.InDbAsync(async db =>
        {
            var widget = new Widget
            {
                WorkspaceId = user.WorkspaceId,
                Type = WidgetType.Carousel,
                Name = "Event Widget"
            };
            db.Widgets.Add(widget);
            await db.SaveChangesAsync();
            widgetId = widget.Id;
        });

        var res = await client.PostAsJsonAsync("/api/public/events", new
        {
            widgetId,
            @event = (int)WidgetEventType.View,
            country = "US",
            device = "desktop"
        });
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var count = await _factory.InDbAsync(db =>
            db.WidgetEvents.CountAsync(e => e.WidgetId == widgetId && e.Event == WidgetEventType.View));
        count.Should().Be(1);
    }

    [Fact]
    public async Task Analytics_dashboard_returns_correct_shape()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "analytics-dashboard@example.com");

        await _factory.InDbAsync(async db =>
        {
            db.Testimonials.Add(new Testimonial
            {
                WorkspaceId = user.WorkspaceId,
                Status = TestimonialStatus.Approved,
                Type = TestimonialType.Text,
                Content = "Analytics test",
                Source = TestimonialSource.Form,
                Rating = 5,
                Submitter = new TestimonialSubmitter { Name = "Alice" }
            });
            await db.SaveChangesAsync();
        });

        var res = await client.GetAsync(
            $"/api/analytics/dashboard?workspaceId={user.WorkspaceId}&daysBack=30");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        data.GetProperty("totalApproved").GetInt64().Should().BeGreaterThanOrEqualTo(1);
        data.TryGetProperty("submissionsOverTime", out _).Should().BeTrue();
        data.TryGetProperty("ratingDistribution", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Analytics_aggregation_job_creates_daily_rollup()
    {
        var user = await _factory.CreateUserAsync(_factory.CreateHttpsClient(), "analytics-agg@example.com");

        Guid widgetId = default;
        await _factory.InDbAsync(async db =>
        {
            var widget = new Widget
            {
                WorkspaceId = user.WorkspaceId,
                Type = WidgetType.Carousel,
                Name = "Agg Widget"
            };
            db.Widgets.Add(widget);
            await db.SaveChangesAsync();
            widgetId = widget.Id;

            // Add yesterday's events.
            var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
            db.WidgetEvents.Add(new WidgetEvent
            {
                WidgetId = widgetId,
                WorkspaceId = user.WorkspaceId,
                Event = WidgetEventType.View,
                Country = "US",
                Device = "mobile",
                OccurredAt = yesterday
            });
            db.WidgetEvents.Add(new WidgetEvent
            {
                WidgetId = widgetId,
                WorkspaceId = user.WorkspaceId,
                Event = WidgetEventType.Click,
                Country = "US",
                Device = "mobile",
                OccurredAt = yesterday
            });
            await db.SaveChangesAsync();
        });

        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<AggregateWidgetAnalyticsJob>();
        await job.RunAsync();

        var daily = await _factory.InDbAsync(db =>
            db.WidgetAnalyticsDailies
                .FirstOrDefaultAsync(d => d.WidgetId == widgetId));
        daily.Should().NotBeNull();
        daily!.Views.Should().Be(1);
        daily.Clicks.Should().Be(1);
    }

    [Fact]
    public async Task Analytics_csv_export_returns_csv_content()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "analytics-csv@example.com");

        var res = await client.GetAsync(
            $"/api/analytics/export/csv?workspaceId={user.WorkspaceId}&daysBack=7");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        var content = await res.Content.ReadAsStringAsync();
        content.Should().Contain("Date,Submissions,Impressions");
    }
}
