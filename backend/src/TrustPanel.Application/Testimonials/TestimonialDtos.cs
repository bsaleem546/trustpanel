using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Application.Testimonials;

public sealed record SubmitterDto(
    string Name, string? Email, string? Company, string? JobTitle, string? AvatarPath)
{
    public static SubmitterDto From(TestimonialSubmitter submitter) => new(
        submitter.Name, submitter.Email, submitter.Company,
        submitter.JobTitle, submitter.AvatarPath);
}

public sealed record TestimonialDto(
    Guid Id,
    Guid WorkspaceId,
    Guid? CollectionFormId,
    TestimonialType Type,
    string Content,
    string? VideoPath,
    string? ThumbnailPath,
    int? Rating,
    TestimonialStatus Status,
    TestimonialSource Source,
    SubmitterDto Submitter,
    double? SentimentScore,
    string? Highlight,
    IReadOnlyList<string> Tags,
    DateTimeOffset? FeaturedAt,
    DateTimeOffset? EditedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static TestimonialDto From(Testimonial testimonial) => new(
        testimonial.Id,
        testimonial.WorkspaceId,
        testimonial.CollectionFormId,
        testimonial.Type,
        testimonial.Content,
        testimonial.VideoPath,
        testimonial.ThumbnailPath,
        testimonial.Rating,
        testimonial.Status,
        testimonial.Source,
        SubmitterDto.From(testimonial.Submitter),
        testimonial.SentimentScore,
        testimonial.Highlight,
        testimonial.Tags,
        testimonial.FeaturedAt,
        testimonial.EditedAt,
        testimonial.CreatedAt,
        testimonial.UpdatedAt);
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
