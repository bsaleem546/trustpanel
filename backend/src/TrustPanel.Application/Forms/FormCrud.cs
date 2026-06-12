using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Forms;

namespace TrustPanel.Application.Forms;

public sealed record ListFormsQuery(Guid UserId, Guid WorkspaceId)
    : IRequest<IReadOnlyList<CollectionFormDto>>;

public sealed class ListFormsQueryHandler
    : IRequestHandler<ListFormsQuery, IReadOnlyList<CollectionFormDto>>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public ListFormsQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<IReadOnlyList<CollectionFormDto>> Handle(
        ListFormsQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireMemberAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var forms = await _db.CollectionForms
            .Where(f => f.WorkspaceId == request.WorkspaceId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        return forms.Select(CollectionFormDto.From).ToList();
    }
}

public sealed record GetFormQuery(Guid UserId, Guid FormId) : IRequest<CollectionFormDto>;

public sealed class GetFormQueryHandler : IRequestHandler<GetFormQuery, CollectionFormDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public GetFormQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<CollectionFormDto> Handle(GetFormQuery request, CancellationToken cancellationToken)
    {
        var form = await _db.CollectionForms
            .FirstOrDefaultAsync(f => f.Id == request.FormId, cancellationToken)
            ?? throw new NotFoundException("Form not found.");
        await _access.RequireMemberAsync(form.WorkspaceId, request.UserId, cancellationToken);
        return CollectionFormDto.From(form);
    }
}

/// <summary>Shared payload for form create/update. Null config sections keep current values.</summary>
public sealed record FormConfigPayload(
    string Name,
    SubmissionType? AllowedSubmissionType,
    QuestionConfigDto? QuestionConfig,
    ThankYouConfigDto? ThankYouConfig,
    RewardConfigDto? RewardConfig,
    bool? IsActive);

public sealed record CreateFormCommand(Guid UserId, Guid WorkspaceId, FormConfigPayload Payload)
    : IRequest<CollectionFormDto>;

public sealed class CreateFormCommandValidator : AbstractValidator<CreateFormCommand>
{
    public CreateFormCommandValidator()
    {
        RuleFor(c => c.Payload.Name).NotEmpty().MaximumLength(128).OverridePropertyName("name");
        RuleFor(c => c.Payload.ThankYouConfig!.RedirectUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .When(c => !string.IsNullOrEmpty(c.Payload.ThankYouConfig?.RedirectUrl))
            .OverridePropertyName("redirectUrl")
            .WithMessage("Redirect URL must be an absolute http(s) URL.");
    }
}

public sealed class CreateFormCommandHandler : IRequestHandler<CreateFormCommand, CollectionFormDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public CreateFormCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<CollectionFormDto> Handle(
        CreateFormCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var form = new CollectionForm
        {
            WorkspaceId = request.WorkspaceId,
            Name = request.Payload.Name,
            Slug = await UniqueSlugAsync(request.WorkspaceId, request.Payload.Name, cancellationToken)
        };
        FormPayloadMapper.Apply(form, request.Payload);

        _db.CollectionForms.Add(form);
        await _db.SaveChangesAsync(cancellationToken);
        return CollectionFormDto.From(form);
    }

    private async Task<string> UniqueSlugAsync(
        Guid workspaceId, string name, CancellationToken cancellationToken)
    {
        var baseSlug = WorkspaceFactory.Slugify(name);
        var slug = baseSlug;
        var attempt = 1;
        while (await _db.CollectionForms.AnyAsync(
            f => f.WorkspaceId == workspaceId && f.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{++attempt}";
        }

        return slug;
    }
}

public sealed record UpdateFormCommand(Guid UserId, Guid FormId, FormConfigPayload Payload)
    : IRequest<CollectionFormDto>;

public sealed class UpdateFormCommandValidator : AbstractValidator<UpdateFormCommand>
{
    public UpdateFormCommandValidator()
    {
        RuleFor(c => c.Payload.Name).NotEmpty().MaximumLength(128).OverridePropertyName("name");
        RuleFor(c => c.Payload.ThankYouConfig!.RedirectUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .When(c => !string.IsNullOrEmpty(c.Payload.ThankYouConfig?.RedirectUrl))
            .OverridePropertyName("redirectUrl")
            .WithMessage("Redirect URL must be an absolute http(s) URL.");
    }
}

public sealed class UpdateFormCommandHandler : IRequestHandler<UpdateFormCommand, CollectionFormDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public UpdateFormCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<CollectionFormDto> Handle(
        UpdateFormCommand request, CancellationToken cancellationToken)
    {
        var form = await _db.CollectionForms
            .FirstOrDefaultAsync(f => f.Id == request.FormId, cancellationToken)
            ?? throw new NotFoundException("Form not found.");
        await _access.RequireManageAsync(form.WorkspaceId, request.UserId, cancellationToken);

        form.Name = request.Payload.Name;
        FormPayloadMapper.Apply(form, request.Payload);
        form.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return CollectionFormDto.From(form);
    }
}

public sealed record DeleteFormCommand(Guid UserId, Guid FormId) : IRequest;

public sealed class DeleteFormCommandHandler : IRequestHandler<DeleteFormCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public DeleteFormCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task Handle(DeleteFormCommand request, CancellationToken cancellationToken)
    {
        var form = await _db.CollectionForms
            .FirstOrDefaultAsync(f => f.Id == request.FormId, cancellationToken)
            ?? throw new NotFoundException("Form not found.");
        await _access.RequireManageAsync(form.WorkspaceId, request.UserId, cancellationToken);

        _db.CollectionForms.Remove(form);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

internal static class FormPayloadMapper
{
    public static void Apply(CollectionForm form, FormConfigPayload payload)
    {
        form.AllowedSubmissionType = payload.AllowedSubmissionType ?? form.AllowedSubmissionType;
        form.IsActive = payload.IsActive ?? form.IsActive;

        if (payload.QuestionConfig is { } questions)
        {
            form.QuestionConfig = new QuestionConfig
            {
                WelcomeTitle = questions.WelcomeTitle,
                WelcomeMessage = questions.WelcomeMessage,
                Prompt = questions.Prompt,
                CollectName = questions.CollectName,
                CollectEmail = questions.CollectEmail,
                CollectCompany = questions.CollectCompany,
                CollectJobTitle = questions.CollectJobTitle,
                CollectAvatar = questions.CollectAvatar,
                CollectRating = questions.CollectRating,
                RequireEmail = questions.RequireEmail
            };
        }

        if (payload.ThankYouConfig is { } thankYou)
        {
            form.ThankYouConfig = new ThankYouConfig
            {
                Title = thankYou.Title,
                Message = thankYou.Message,
                RedirectUrl = thankYou.RedirectUrl,
                ShowSocialShare = thankYou.ShowSocialShare
            };
        }

        if (payload.RewardConfig is { } reward)
        {
            form.RewardConfig = new RewardConfig
            {
                Enabled = reward.Enabled,
                Description = reward.Description,
                CouponCode = reward.CouponCode,
                RewardUrl = reward.RewardUrl
            };
        }
    }
}
