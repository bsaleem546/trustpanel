using TrustPanel.Domain.Forms;

namespace TrustPanel.Application.Forms;

public sealed record QuestionConfigDto(
    string WelcomeTitle,
    string WelcomeMessage,
    string Prompt,
    bool CollectName,
    bool CollectEmail,
    bool CollectCompany,
    bool CollectJobTitle,
    bool CollectAvatar,
    bool CollectRating,
    bool RequireEmail)
{
    public static QuestionConfigDto From(QuestionConfig config) => new(
        config.WelcomeTitle, config.WelcomeMessage, config.Prompt,
        config.CollectName, config.CollectEmail, config.CollectCompany,
        config.CollectJobTitle, config.CollectAvatar, config.CollectRating,
        config.RequireEmail);
}

public sealed record ThankYouConfigDto(
    string Title, string Message, string? RedirectUrl, bool ShowSocialShare)
{
    public static ThankYouConfigDto From(ThankYouConfig config)
        => new(config.Title, config.Message, config.RedirectUrl, config.ShowSocialShare);
}

public sealed record RewardConfigDto(
    bool Enabled, string? Description, string? CouponCode, string? RewardUrl)
{
    public static RewardConfigDto From(RewardConfig config)
        => new(config.Enabled, config.Description, config.CouponCode, config.RewardUrl);
}

public sealed record CollectionFormDto(
    Guid Id,
    Guid WorkspaceId,
    string Slug,
    string Name,
    SubmissionType AllowedSubmissionType,
    QuestionConfigDto QuestionConfig,
    ThankYouConfigDto ThankYouConfig,
    RewardConfigDto RewardConfig,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static CollectionFormDto From(CollectionForm form) => new(
        form.Id,
        form.WorkspaceId,
        form.Slug,
        form.Name,
        form.AllowedSubmissionType,
        QuestionConfigDto.From(form.QuestionConfig),
        ThankYouConfigDto.From(form.ThankYouConfig),
        RewardConfigDto.From(form.RewardConfig),
        form.IsActive,
        form.CreatedAt,
        form.UpdatedAt);
}

/// <summary>What an anonymous visitor needs to render the collection form. No reward details.</summary>
public sealed record PublicFormDto(
    Guid FormId,
    Guid WorkspaceId,
    string FormSlug,
    string Name,
    SubmissionType AllowedSubmissionType,
    QuestionConfigDto Questions,
    string WorkspaceName,
    string? LogoPath,
    string PrimaryColor,
    string SecondaryColor,
    string FontFamily,
    bool ShowTrustPanelBranding);
