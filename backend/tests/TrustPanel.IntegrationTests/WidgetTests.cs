using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Domain.Testimonials;
using TrustPanel.Domain.Widgets;

namespace TrustPanel.IntegrationTests;

public sealed class WidgetTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public WidgetTests(PostgresApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Widget_crud_round_trip()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "widget-crud@example.com");

        var create = await client.PostAsJsonAsync("/api/widgets", new
        {
            workspaceId = user.WorkspaceId,
            type = (int)WidgetType.Carousel,
            name = "Homepage Reviews"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var widgetId = (await create.ReadDataAsync()).GetProperty("id").GetGuid();

        var get = await client.GetAsync($"/api/widgets/{widgetId}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        (await get.ReadDataAsync()).GetProperty("name").GetString().Should().Be("Homepage Reviews");

        var update = await client.PutAsJsonAsync($"/api/widgets/{widgetId}", new
        {
            workspaceId = user.WorkspaceId,
            type = (int)WidgetType.Carousel,
            name = "Updated Reviews"
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        (await update.ReadDataAsync()).GetProperty("name").GetString().Should().Be("Updated Reviews");

        var delete = await client.DeleteAsync($"/api/widgets/{widgetId}");
        delete.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDelete = await client.GetAsync($"/api/widgets/{widgetId}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Public_widget_returns_approved_testimonials_only()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "widget-public@example.com");

        // Add approved and pending testimonials.
        Guid widgetId = default;
        await _factory.InDbAsync(async db =>
        {
            db.Testimonials.Add(new Testimonial
            {
                WorkspaceId = user.WorkspaceId, Status = TestimonialStatus.Approved,
                Type = TestimonialType.Text, Content = "Great!",
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

            var widget = new Widget
            {
                WorkspaceId = user.WorkspaceId,
                Type = WidgetType.Carousel,
                Name = "Test Widget"
            };
            db.Widgets.Add(widget);
            await db.SaveChangesAsync();
            widgetId = widget.Id;
        });

        var res = await client.GetAsync($"/api/public/widget/{widgetId}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await res.ReadDataAsync();
        var testimonials = data.GetProperty("testimonials");
        testimonials.GetArrayLength().Should().Be(1);
        testimonials[0].GetProperty("submitterName").GetString().Should().Be("Alice");
    }

    [Fact]
    public async Task Widget_filters_by_minimum_rating()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "widget-filter-rating@example.com");

        Guid widgetId = default;
        await _factory.InDbAsync(async db =>
        {
            db.Testimonials.Add(new Testimonial
            {
                WorkspaceId = user.WorkspaceId, Status = TestimonialStatus.Approved,
                Type = TestimonialType.Text, Content = "5 star!",
                Source = TestimonialSource.Form, Rating = 5,
                Submitter = new TestimonialSubmitter { Name = "HighRater" }
            });
            db.Testimonials.Add(new Testimonial
            {
                WorkspaceId = user.WorkspaceId, Status = TestimonialStatus.Approved,
                Type = TestimonialType.Text, Content = "2 star",
                Source = TestimonialSource.Form, Rating = 2,
                Submitter = new TestimonialSubmitter { Name = "LowRater" }
            });

            var widget = new Widget
            {
                WorkspaceId = user.WorkspaceId,
                Type = WidgetType.Carousel,
                Name = "High Rated Only",
                MinimumRating = 4
            };
            db.Widgets.Add(widget);
            await db.SaveChangesAsync();
            widgetId = widget.Id;
        });

        var res = await client.GetAsync($"/api/public/widget/{widgetId}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var testimonials = (await res.ReadDataAsync()).GetProperty("testimonials");
        testimonials.GetArrayLength().Should().Be(1);
        testimonials[0].GetProperty("submitterName").GetString().Should().Be("HighRater");
    }

    [Fact]
    public async Task Cache_bust_on_approve_causes_fresh_response()
    {
        var client = _factory.CreateHttpsClient();
        var user = await _factory.CreateUserAsync(client, "widget-cache-bust@example.com");

        Guid widgetId = default;
        Guid testimonialId = default;
        await _factory.InDbAsync(async db =>
        {
            var t = new Testimonial
            {
                WorkspaceId = user.WorkspaceId, Status = TestimonialStatus.Pending,
                Type = TestimonialType.Text, Content = "Cache test",
                Source = TestimonialSource.Form,
                Submitter = new TestimonialSubmitter { Name = "CacheUser" }
            };
            db.Testimonials.Add(t);

            var widget = new Widget
            {
                WorkspaceId = user.WorkspaceId, Type = WidgetType.Carousel, Name = "Cache Widget"
            };
            db.Widgets.Add(widget);
            await db.SaveChangesAsync();
            widgetId = widget.Id;
            testimonialId = t.Id;
        });

        // First fetch caches empty list (pending testimonial not included).
        var first = await client.GetAsync($"/api/public/widget/{widgetId}");
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        (await first.ReadDataAsync()).GetProperty("testimonials").GetArrayLength().Should().Be(0);

        // Approve the testimonial — cache should be busted.
        var approve = await client.PostAsync($"/api/testimonials/{testimonialId}/approve", null);
        approve.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second fetch should now return the approved testimonial.
        var second = await client.GetAsync($"/api/public/widget/{widgetId}");
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await second.ReadDataAsync()).GetProperty("testimonials").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Tenant_isolation_widget_not_accessible_from_other_workspace()
    {
        var client1 = _factory.CreateHttpsClient();
        var client2 = _factory.CreateHttpsClient();
        var user1 = await _factory.CreateUserAsync(client1, "widget-isolation-1@example.com");
        var user2 = await _factory.CreateUserAsync(client2, "widget-isolation-2@example.com");

        var create = await client1.PostAsJsonAsync("/api/widgets", new
        {
            workspaceId = user1.WorkspaceId,
            type = (int)WidgetType.Carousel,
            name = "User1 Widget"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var widgetId = (await create.ReadDataAsync()).GetProperty("id").GetGuid();

        // user2 should get 404 (workspace ownership check fails).
        var getAsUser2 = await client2.GetAsync($"/api/widgets/{widgetId}");
        getAsUser2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
