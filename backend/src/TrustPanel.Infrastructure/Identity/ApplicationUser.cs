using Microsoft.AspNetCore.Identity;
using TrustPanel.Domain.Users;

namespace TrustPanel.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? PlanId { get; set; }
    public string? StripeCustomerId { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool OnboardingCompleted { get; set; }
    public string OnboardingState { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
