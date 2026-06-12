using TrustPanel.Domain.Common;

namespace TrustPanel.Domain.Testimonials;

public class Testimonial : Entity
{
    public Guid WorkspaceId { get; set; }
    public Guid? CollectionFormId { get; set; }
    public TestimonialType Type { get; set; } = TestimonialType.Text;
    public string Content { get; set; } = string.Empty;
    public string? VideoPath { get; set; }
    public string? ThumbnailPath { get; set; }
    public int? Rating { get; set; }
    public TestimonialStatus Status { get; set; } = TestimonialStatus.Pending;
    public TestimonialSource Source { get; set; } = TestimonialSource.Form;
    public TestimonialSubmitter Submitter { get; set; } = new();
    public double? SentimentScore { get; set; }
    public string? Highlight { get; set; }
    public List<string> Tags { get; set; } = [];
    public DateTimeOffset? FeaturedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
