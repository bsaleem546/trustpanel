using System.Linq.Expressions;

namespace TrustPanel.Application.Common;

/// <summary>
/// Dispatches background work outside the request path. Implemented by Hangfire in
/// Infrastructure; replaced with a capturing fake in tests.
/// </summary>
public interface IJobScheduler
{
    void Enqueue<TJob>(Expression<Func<TJob, Task>> job) where TJob : class;
    void Schedule<TJob>(Expression<Func<TJob, Task>> job, TimeSpan delay) where TJob : class;
}
