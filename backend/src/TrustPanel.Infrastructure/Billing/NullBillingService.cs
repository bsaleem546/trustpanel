using TrustPanel.Application.Billing;

namespace TrustPanel.Infrastructure.Billing;

/// <summary>Billing stub used when Stripe is not configured.</summary>
public sealed class NullBillingService : IBillingService
{
    public Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid userId, string priceId, string successUrl, string cancelUrl,
        CancellationToken cancellationToken)
        => Task.FromResult(new CheckoutSessionResult(successUrl));

    public Task<PortalSessionResult> CreatePortalSessionAsync(
        Guid userId, string returnUrl, CancellationToken cancellationToken)
        => Task.FromResult(new PortalSessionResult(returnUrl));

    public Task HandleWebhookAsync(
        string payload, string signature, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
