using System.Security.Cryptography;
using TrustPanel.Domain.Teams;
using TrustPanel.Domain.Workspaces;

namespace TrustPanel.Application.Workspaces;

public static class WorkspaceFactory
{
    /// <summary>Creates the default workspace plus the owner membership for a new user.</summary>
    public static (Workspace Workspace, WorkspaceMember OwnerMembership) CreateDefault(
        Guid ownerUserId, string email, string? name = null)
    {
        var localPart = email.Split('@')[0];
        var baseSlug = Slugify(string.IsNullOrWhiteSpace(name) ? localPart : name);

        var workspace = new Workspace
        {
            OwnerUserId = ownerUserId,
            Name = string.IsNullOrWhiteSpace(name) ? $"{localPart}'s workspace" : name,
            Slug = $"{baseSlug}-{RandomSuffix()}"
        };

        var membership = new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = ownerUserId,
            Role = WorkspaceRole.Owner,
            AcceptedAt = DateTimeOffset.UtcNow
        };

        return (workspace, membership);
    }

    public static string Slugify(string value)
    {
        var slug = new string(value
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray())
            .Trim('-');

        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        return string.IsNullOrEmpty(slug) ? "workspace" : slug[..Math.Min(slug.Length, 40)];
    }

    private static string RandomSuffix()
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
        return string.Create(6, alphabet, (span, chars) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }
        });
    }
}
