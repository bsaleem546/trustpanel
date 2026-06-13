namespace TrustPanel.Application.Billing;

public sealed record CheckoutSessionResult(string Url);
public sealed record PortalSessionResult(string Url);

public interface IBillingService
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid userId, string priceId, string successUrl, string cancelUrl,
        CancellationToken cancellationToken);

    Task<PortalSessionResult> CreatePortalSessionAsync(
        Guid userId, string returnUrl, CancellationToken cancellationToken);

    Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken);
}
