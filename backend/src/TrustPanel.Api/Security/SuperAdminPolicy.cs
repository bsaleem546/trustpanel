using Microsoft.AspNetCore.Authorization;
using TrustPanel.Domain.Users;

namespace TrustPanel.Api.Security;

public static class SuperAdminPolicy
{
    public const string Name = "SuperAdmin";

    public static AuthorizationPolicy Build()
        => new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole(nameof(UserRole.SuperAdmin))
            .Build();
}
