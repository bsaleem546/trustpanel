using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;

namespace TrustPanel.Application.Auth.Commands;

/// <summary>
/// Persists multi-step onboarding progress: workspace name, logo, first form
/// template choice, and whether the embed snippet was viewed.
/// </summary>
public sealed record UpdateOnboardingCommand(
    Guid UserId,
    string? WorkspaceName,
    string? LogoPath,
    string? FirstFormTemplate,
    bool? EmbedSnippetViewed,
    bool Completed) : IRequest<OnboardingStateDto>;

public sealed record OnboardingStateDto(
    string? WorkspaceName,
    string? LogoPath,
    string? FirstFormTemplate,
    bool EmbedSnippetViewed,
    bool Completed);

public sealed class UpdateOnboardingCommandValidator : AbstractValidator<UpdateOnboardingCommand>
{
    public UpdateOnboardingCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.WorkspaceName).MaximumLength(128);
        RuleFor(c => c.LogoPath).MaximumLength(512);
        RuleFor(c => c.FirstFormTemplate).MaximumLength(64);
    }
}

public sealed class UpdateOnboardingCommandHandler
    : IRequestHandler<UpdateOnboardingCommand, OnboardingStateDto>
{
    private readonly IIdentityService _identityService;
    private readonly IAppDbContext _db;

    public UpdateOnboardingCommandHandler(IIdentityService identityService, IAppDbContext db)
    {
        _identityService = identityService;
        _db = db;
    }

    public async Task<OnboardingStateDto> Handle(
        UpdateOnboardingCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.FindByIdAsync(request.UserId)
            ?? throw new NotFoundException("User not found.");

        var existing = Deserialize(user.OnboardingState);
        var updated = new OnboardingStateDto(
            request.WorkspaceName ?? existing.WorkspaceName,
            request.LogoPath ?? existing.LogoPath,
            request.FirstFormTemplate ?? existing.FirstFormTemplate,
            request.EmbedSnippetViewed ?? existing.EmbedSnippetViewed,
            request.Completed || existing.Completed);

        if (!string.IsNullOrWhiteSpace(request.WorkspaceName) || !string.IsNullOrWhiteSpace(request.LogoPath))
        {
            var workspace = await _db.Workspaces
                .Where(w => w.OwnerUserId == request.UserId)
                .OrderBy(w => w.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (workspace is not null)
            {
                if (!string.IsNullOrWhiteSpace(request.WorkspaceName))
                {
                    workspace.Name = request.WorkspaceName;
                }

                if (!string.IsNullOrWhiteSpace(request.LogoPath))
                {
                    workspace.Branding.LogoPath = request.LogoPath;
                }

                workspace.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        await _identityService.UpdateOnboardingAsync(
            request.UserId, JsonSerializer.Serialize(updated), updated.Completed);

        return updated;
    }

    private static OnboardingStateDto Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<OnboardingStateDto>(json)
                ?? new OnboardingStateDto(null, null, null, false, false);
        }
        catch (JsonException)
        {
            return new OnboardingStateDto(null, null, null, false, false);
        }
    }
}
