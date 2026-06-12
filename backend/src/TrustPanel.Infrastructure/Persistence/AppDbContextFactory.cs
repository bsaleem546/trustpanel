using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TrustPanel.Application.Common;

namespace TrustPanel.Infrastructure.Persistence;

/// <summary>Design-time factory for `dotnet ef` commands. Never used at runtime.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__DEFAULT")
            ?? "Host=localhost;Port=5432;Database=trustpanel;Username=trustpanel;Password=change-me";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options, new WorkspaceContext());
    }
}
