using System.Linq.Expressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using TrustPanel.Application.Auth;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;
using TrustPanel.Infrastructure.Persistence;

namespace TrustPanel.IntegrationTests;

/// <summary>Boots the API against a disposable PostgreSQL container with migrations applied.</summary>
public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    public CapturingAuthEmailSender AuthEmails { get; } = new();
    public FakeDnsResolver Dns { get; } = new();
    public CapturingJobScheduler Jobs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("environment", "Testing");
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAuthEmailSender>();
            services.AddSingleton<IAuthEmailSender>(AuthEmails);
            services.RemoveAll<IDnsResolver>();
            services.AddSingleton<IDnsResolver>(Dns);
            services.RemoveAll<IJobScheduler>();
            services.AddSingleton<IJobScheduler>(Jobs);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        // https so the Secure refresh cookie round-trips in tests.
        BaseAddress = new Uri("https://localhost")
    });
}

public sealed class CapturingAuthEmailSender : IAuthEmailSender
{
    public List<(string Email, Guid UserId, string Token)> ConfirmationEmails { get; } = [];
    public List<(string Email, string Token)> PasswordResetEmails { get; } = [];

    public Task SendEmailConfirmationAsync(
        string email, Guid userId, string token, CancellationToken cancellationToken)
    {
        ConfirmationEmails.Add((email, userId, token));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken)
    {
        PasswordResetEmails.Add((email, token));
        return Task.CompletedTask;
    }
}

/// <summary>In-memory CNAME table; tests point domains at the configured target.</summary>
public sealed class FakeDnsResolver : IDnsResolver
{
    private readonly Dictionary<string, string[]> _cnames = new(StringComparer.OrdinalIgnoreCase);

    public void SetCname(string host, params string[] targets) => _cnames[host] = targets;

    public void Clear(string host) => _cnames.Remove(host);

    public Task<IReadOnlyList<string>> GetCnameRecordsAsync(
        string host, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>(
            _cnames.TryGetValue(host, out var targets) ? targets : []);
}

/// <summary>Records enqueued background jobs instead of running them.</summary>
public sealed class CapturingJobScheduler : IJobScheduler
{
    public List<(Type JobType, LambdaExpression Call)> Enqueued { get; } = [];
    public List<(Type JobType, LambdaExpression Call, TimeSpan Delay)> Scheduled { get; } = [];

    public void Enqueue<TJob>(Expression<Func<TJob, Task>> job) where TJob : class
        => Enqueued.Add((typeof(TJob), job));

    public void Schedule<TJob>(Expression<Func<TJob, Task>> job, TimeSpan delay) where TJob : class
        => Scheduled.Add((typeof(TJob), job, delay));
}
