using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Email;

namespace TrustPanel.Application.Email;

public interface ITokenProtector
{
    string Protect(string value);
    string? Unprotect(string token);
}

/// <summary>Creates and validates signed one-click unsubscribe tokens.</summary>
public sealed class UnsubscribeService
{
    private readonly ITokenProtector _protector;
    private readonly IAppDbContext _db;

    public UnsubscribeService(ITokenProtector protector, IAppDbContext db)
    {
        _protector = protector;
        _db = db;
    }

    public string CreateToken(string email) => _protector.Protect(email);

    public async Task<bool> ProcessUnsubscribeAsync(string token, CancellationToken cancellationToken)
    {
        var email = _protector.Unprotect(token);
        if (email is null) return false;

        var exists = await _db.EmailSuppressions
            .AnyAsync(s => s.Email == email, cancellationToken);
        if (!exists)
        {
            _db.EmailSuppressions.Add(new EmailSuppression
            {
                WorkspaceId = Guid.Empty,
                Email = email,
                Reason = SuppressionReason.Unsubscribed,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        return true;
    }
}
