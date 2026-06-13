using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TrustPanel.Application.Admin;
using TrustPanel.Application.Common;

namespace TrustPanel.Infrastructure.Identity;

public sealed class AdminUserLookup : IAdminUserLookup
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAppDbContext _db;

    public AdminUserLookup(UserManager<ApplicationUser> userManager, IAppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<(string Email, Guid? WorkspaceId)?> FindUserAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user?.Email is null) return null;

        var workspaceId = await _db.Workspaces
            .Where(w => w.OwnerUserId == userId)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return (user.Email, workspaceId);
    }
}
