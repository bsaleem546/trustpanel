using MediatR;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Testimonials;

namespace TrustPanel.Application.Testimonials;

public sealed record ListTestimonialsQuery(
    Guid UserId,
    Guid WorkspaceId,
    TestimonialStatus? Status,
    string? Tag,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<TestimonialDto>>;

public sealed class ListTestimonialsQueryHandler
    : IRequestHandler<ListTestimonialsQuery, PagedResult<TestimonialDto>>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public ListTestimonialsQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<PagedResult<TestimonialDto>> Handle(
        ListTestimonialsQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireMemberAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Testimonials.Where(t => t.WorkspaceId == request.WorkspaceId);
        if (request.Status is { } status)
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            query = query.Where(t => t.Tags.Contains(request.Tag));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TestimonialDto>(
            items.Select(TestimonialDto.From).ToList(), total, page, pageSize);
    }
}

public sealed record GetTestimonialQuery(Guid UserId, Guid TestimonialId)
    : IRequest<TestimonialDto>;

public sealed class GetTestimonialQueryHandler
    : IRequestHandler<GetTestimonialQuery, TestimonialDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public GetTestimonialQueryHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<TestimonialDto> Handle(
        GetTestimonialQuery request, CancellationToken cancellationToken)
    {
        var testimonial = await _db.Testimonials
            .FirstOrDefaultAsync(t => t.Id == request.TestimonialId, cancellationToken)
            ?? throw new NotFoundException("Testimonial not found.");
        await _access.RequireMemberAsync(testimonial.WorkspaceId, request.UserId, cancellationToken);
        return TestimonialDto.From(testimonial);
    }
}

public sealed record SearchTestimonialsQuery(
    Guid UserId, Guid WorkspaceId, string Query, int Limit = 25)
    : IRequest<IReadOnlyList<TestimonialDto>>;

public sealed class SearchTestimonialsQueryHandler
    : IRequestHandler<SearchTestimonialsQuery, IReadOnlyList<TestimonialDto>>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;
    private readonly ISearchIndexer _searchIndexer;

    public SearchTestimonialsQueryHandler(
        IAppDbContext db, WorkspaceAccess access, ISearchIndexer searchIndexer)
    {
        _db = db;
        _access = access;
        _searchIndexer = searchIndexer;
    }

    public async Task<IReadOnlyList<TestimonialDto>> Handle(
        SearchTestimonialsQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireMemberAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var limit = Math.Clamp(request.Limit, 1, 100);
        var ids = await _searchIndexer.SearchAsync(
            request.WorkspaceId, request.Query, limit, cancellationToken);

        List<Testimonial> testimonials;
        if (ids is not null)
        {
            var loaded = await _db.Testimonials
                .Where(t => ids.Contains(t.Id) && t.WorkspaceId == request.WorkspaceId)
                .ToListAsync(cancellationToken);
            // Preserve relevance order from the search backend.
            testimonials = ids
                .Select(id => loaded.FirstOrDefault(t => t.Id == id))
                .Where(t => t is not null)
                .Select(t => t!)
                .ToList();
        }
        else
        {
            // Search backend unavailable: SQL substring fallback.
            var pattern = $"%{request.Query}%";
            testimonials = await _db.Testimonials
                .Where(t => t.WorkspaceId == request.WorkspaceId)
                .Where(t => EF.Functions.ILike(t.Content, pattern)
                    || EF.Functions.ILike(t.Submitter.Name, pattern))
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        return testimonials.Select(TestimonialDto.From).ToList();
    }
}
