using MediatR;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;

namespace TrustPanel.Application.Ai;

public sealed record GetInsightsQuery(Guid UserId, Guid WorkspaceId)
    : IRequest<InsightsReport?>;

public sealed class GetInsightsQueryHandler : IRequestHandler<GetInsightsQuery, InsightsReport?>
{
    private readonly ICacheService _cache;
    private readonly IJobScheduler _jobs;
    private readonly WorkspaceAccess _access;

    public GetInsightsQueryHandler(ICacheService cache, IJobScheduler jobs, WorkspaceAccess access)
    {
        _cache = cache;
        _jobs = jobs;
        _access = access;
    }

    public async Task<InsightsReport?> Handle(GetInsightsQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireMemberAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var cacheKey = $"insights:{request.WorkspaceId}";
        var cached = await _cache.GetAsync<string>(cacheKey, cancellationToken);
        if (cached is null)
        {
            // Enqueue generation; return null this call (caller shows "generating" state).
            _jobs.Enqueue<IInsightsJobRunner>(j => j.RunAsync(request.WorkspaceId));
            return null;
        }

        return System.Text.Json.JsonSerializer.Deserialize<InsightsReport>(cached);
    }
}

public sealed record GetReplySuggestionQuery(Guid UserId, Guid WorkspaceId, Guid TestimonialId)
    : IRequest<string?>;

public sealed class GetReplySuggestionQueryHandler : IRequestHandler<GetReplySuggestionQuery, string?>
{
    private readonly ICacheService _cache;
    private readonly IJobScheduler _jobs;
    private readonly WorkspaceAccess _access;

    public GetReplySuggestionQueryHandler(ICacheService cache, IJobScheduler jobs, WorkspaceAccess access)
    {
        _cache = cache;
        _jobs = jobs;
        _access = access;
    }

    public async Task<string?> Handle(GetReplySuggestionQuery request, CancellationToken cancellationToken)
    {
        await _access.RequireMemberAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var cacheKey = $"reply-suggestion:{request.TestimonialId}";
        var cached = await _cache.GetAsync<string>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        // Enqueue and return null (client polls).
        _jobs.Enqueue<IReplyJobRunner>(j => j.RunAsync(request.TestimonialId, request.WorkspaceId));
        return null;
    }
}

/// <summary>Job runner interface for insights generation (implemented in Infrastructure).</summary>
public interface IInsightsJobRunner
{
    Task RunAsync(Guid workspaceId);
}

/// <summary>Job runner interface for reply suggestion (implemented in Infrastructure).</summary>
public interface IReplyJobRunner
{
    Task RunAsync(Guid testimonialId, Guid workspaceId);
}
