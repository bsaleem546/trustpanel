using System.Linq.Expressions;
using Hangfire;
using Microsoft.Extensions.Logging;
using TrustPanel.Application.Common;

namespace TrustPanel.Infrastructure.Jobs;

public sealed class HangfireJobScheduler : IJobScheduler
{
    private readonly IBackgroundJobClient _client;

    public HangfireJobScheduler(IBackgroundJobClient client)
    {
        _client = client;
    }

    public void Enqueue<TJob>(Expression<Func<TJob, Task>> job) where TJob : class
        => _client.Enqueue(job);

    public void Schedule<TJob>(Expression<Func<TJob, Task>> job, TimeSpan delay) where TJob : class
        => _client.Schedule(job, delay);
}

/// <summary>Used when Hangfire storage is not configured (tests, bare local runs).</summary>
public sealed class NullJobScheduler : IJobScheduler
{
    private readonly ILogger<NullJobScheduler> _logger;

    public NullJobScheduler(ILogger<NullJobScheduler> logger)
    {
        _logger = logger;
    }

    public void Enqueue<TJob>(Expression<Func<TJob, Task>> job) where TJob : class
        => _logger.LogWarning("Background job {Job} discarded: no job storage configured.", typeof(TJob).Name);

    public void Schedule<TJob>(Expression<Func<TJob, Task>> job, TimeSpan delay) where TJob : class
        => _logger.LogWarning("Background job {Job} discarded: no job storage configured.", typeof(TJob).Name);
}
