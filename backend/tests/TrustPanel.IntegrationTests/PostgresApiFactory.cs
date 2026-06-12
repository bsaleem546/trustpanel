using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using TrustPanel.Application.Auth;
using TrustPanel.Infrastructure.Persistence;

namespace TrustPanel.IntegrationTests;

/// <summary>Boots the API against a disposable PostgreSQL container with migrations applied.</summary>
public class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    public CapturingAuthEmailSender AuthEmails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("environment", "Testing");
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAuthEmailSender>();
            services.AddSingleton<IAuthEmailSender>(AuthEmails);
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
