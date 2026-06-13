using Microsoft.AspNetCore.DataProtection;
using TrustPanel.Application.Email;

namespace TrustPanel.Infrastructure.Security;

public sealed class DataProtectionTokenProtector : ITokenProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionTokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("email-unsubscribe");
    }

    public string Protect(string value) => _protector.Protect(value);

    public string? Unprotect(string token)
    {
        try { return _protector.Unprotect(token); }
        catch { return null; }
    }
}
