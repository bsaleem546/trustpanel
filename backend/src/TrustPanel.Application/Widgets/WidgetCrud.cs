using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Billing;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Testimonials;
using TrustPanel.Domain.Widgets;
using TrustPanel.Domain.Workspaces;

namespace TrustPanel.Application.Widgets;

// ── List ──────────────────────────────────────────────────────────────────────

public sealed record ListWidgetsQuery(Guid UserId, Guid WorkspaceId)
    : IRequest<IReadOnlyList<WidgetDto>>;

public sealed class ListWidgetsQueryHandler
    : IRequestHandler<ListWidgetsQuery, IReadOnlyList<WidgetDto>>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public ListWidgetsQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<IReadOnlyList<WidgetDto>> Handle(
        ListWidgetsQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireMemberAsync(request.WorkspaceId, request.UserId, cancellationToken);
        var widgets = await _db.Widgets
            .Where(w => w.WorkspaceId == request.WorkspaceId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
        return widgets.Select(WidgetDto.From).ToList();
    }
}

// ── Get ───────────────────────────────────────────────────────────────────────

public sealed record GetWidgetQuery(Guid UserId, Guid WidgetId) : IRequest<WidgetDto>;

public sealed class GetWidgetQueryHandler : IRequestHandler<GetWidgetQuery, WidgetDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public GetWidgetQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<WidgetDto> Handle(
        GetWidgetQuery request, CancellationToken cancellationToken)
    {
        var widget = await _db.Widgets
            .FirstOrDefaultAsync(w => w.Id == request.WidgetId, cancellationToken)
            ?? throw new NotFoundException("Widget not found.");
        await _access.RequireMemberAsync(widget.WorkspaceId, request.UserId, cancellationToken);
        return WidgetDto.From(widget);
    }
}

// ── Create ────────────────────────────────────────────────────────────────────

public sealed record WidgetPayload(
    WidgetType Type,
    string Name,
    IReadOnlyList<string>? FilterTags,
    int? MinimumRating,
    bool? FeaturedOnly,
    IReadOnlyList<Guid>? SelectedTestimonialIds,
    TestimonialSource? SourceFilter,
    WidgetSettings? Settings,
    string? CustomCss);

public sealed record CreateWidgetCommand(
    Guid UserId, Guid WorkspaceId, WidgetPayload Payload) : IRequest<WidgetDto>;

public sealed class CreateWidgetCommandHandler : IRequestHandler<CreateWidgetCommand, WidgetDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly IPlanResolver _plan;

    public CreateWidgetCommandHandler(IAppDbContext db, WorkspaceAccess access, IPlanResolver plan)
    {
        _db = db; _access = access; _plan = plan;
    }

    public async Task<WidgetDto> Handle(
        CreateWidgetCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var ownerUserId = await _db.Workspaces
            .Where(w => w.Id == request.WorkspaceId)
            .Select(w => w.OwnerUserId)
            .SingleAsync(cancellationToken);
        var ownerPlan = await _plan.ResolveForUserAsync(ownerUserId, cancellationToken);
        if (ownerPlan.Plan.WidgetLimit >= 0)
        {
            var count = await _db.Widgets.CountAsync(
                w => w.WorkspaceId == request.WorkspaceId, cancellationToken);
            if (count >= ownerPlan.Plan.WidgetLimit)
                throw new ConflictException(
                    $"Widget limit of {ownerPlan.Plan.WidgetLimit} reached on your plan.");
        }

        var widget = new Widget { WorkspaceId = request.WorkspaceId };
        Apply(widget, request.Payload);
        _db.Widgets.Add(widget);
        await _db.SaveChangesAsync(cancellationToken);
        return WidgetDto.From(widget);
    }

    internal static void Apply(Widget widget, WidgetPayload payload)
    {
        widget.Type = payload.Type;
        widget.Name = payload.Name;
        if (payload.FilterTags is not null) widget.FilterTags = payload.FilterTags.ToList();
        widget.MinimumRating = payload.MinimumRating;
        widget.FeaturedOnly = payload.FeaturedOnly ?? false;
        if (payload.SelectedTestimonialIds is not null)
            widget.SelectedTestimonialIds = payload.SelectedTestimonialIds.ToList();
        widget.SourceFilter = payload.SourceFilter;
        if (payload.Settings is not null) widget.Settings = payload.Settings;
        widget.CustomCss = payload.CustomCss;
        widget.UpdatedAt = DateTimeOffset.UtcNow;
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

public sealed record UpdateWidgetCommand(
    Guid UserId, Guid WidgetId, WidgetPayload Payload) : IRequest<WidgetDto>;

public sealed class UpdateWidgetCommandHandler : IRequestHandler<UpdateWidgetCommand, WidgetDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public UpdateWidgetCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<WidgetDto> Handle(
        UpdateWidgetCommand request, CancellationToken cancellationToken)
    {
        var widget = await _db.Widgets
            .FirstOrDefaultAsync(w => w.Id == request.WidgetId, cancellationToken)
            ?? throw new NotFoundException("Widget not found.");
        await _access.RequireManageAsync(widget.WorkspaceId, request.UserId, cancellationToken);
        CreateWidgetCommandHandler.Apply(widget, request.Payload);
        await _db.SaveChangesAsync(cancellationToken);
        return WidgetDto.From(widget);
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

public sealed record DeleteWidgetCommand(Guid UserId, Guid WidgetId) : IRequest;

public sealed class DeleteWidgetCommandHandler : IRequestHandler<DeleteWidgetCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public DeleteWidgetCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task Handle(DeleteWidgetCommand request, CancellationToken cancellationToken)
    {
        var widget = await _db.Widgets
            .FirstOrDefaultAsync(w => w.Id == request.WidgetId, cancellationToken)
            ?? throw new NotFoundException("Widget not found.");
        await _access.RequireManageAsync(widget.WorkspaceId, request.UserId, cancellationToken);
        _db.Widgets.Remove(widget);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
