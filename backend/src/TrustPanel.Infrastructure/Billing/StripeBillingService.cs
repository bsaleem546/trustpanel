using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using TrustPanel.Application.Billing;
using TrustPanel.Application.Common;
using TrustPanel.Domain.Billing;

namespace TrustPanel.Infrastructure.Billing;

public sealed class StripeBillingService : IBillingService
{
    private readonly IAppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(
        IAppDbContext db, IConfiguration configuration, ILogger<StripeBillingService> logger)
    {
        StripeConfiguration.ApiKey = configuration["STRIPE_SECRET_KEY"] ?? string.Empty;
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid userId, string priceId, string successUrl, string cancelUrl,
        CancellationToken cancellationToken)
    {
        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = userId.ToString()
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
        return new CheckoutSessionResult(session.Url);
    }

    public async Task<PortalSessionResult> CreatePortalSessionAsync(
        Guid userId, string returnUrl, CancellationToken cancellationToken)
    {
        var stripeCustomerId = await _db.Subscriptions
            .Where(s => s.UserId == userId)
            .Select(s => s.StripeCustomerId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new Application.Common.NotFoundException("No active subscription found.");

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,
            ReturnUrl = returnUrl
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
        return new PortalSessionResult(session.Url);
    }

    public async Task HandleWebhookAsync(
        string payload, string signature, CancellationToken cancellationToken)
    {
        var webhookSecret = _configuration["STRIPE_WEBHOOK_SECRET"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            _logger.LogWarning("STRIPE_WEBHOOK_SECRET not configured — skipping signature verification");
            return;
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature");
            throw new Application.Common.UnauthorizedAppException("Invalid webhook signature.");
        }

        switch (stripeEvent.Type)
        {
            case "invoice.payment_succeeded":
                await HandlePaymentSucceededAsync(stripeEvent, cancellationToken);
                break;
            case "invoice.payment_failed":
                await HandlePaymentFailedAsync(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken);
                break;
            default:
                _logger.LogDebug("Unhandled Stripe event: {Type}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandlePaymentSucceededAsync(Event e, CancellationToken ct)
    {
        var invoice = e.Data.Object as Invoice;
        if (invoice?.SubscriptionId is null) return;

        var sub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == invoice.SubscriptionId, ct);
        if (sub is null) return;

        sub.Status = SubscriptionStatus.Active;
        sub.GracePeriodEndsAt = null;
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandlePaymentFailedAsync(Event e, CancellationToken ct)
    {
        var invoice = e.Data.Object as Invoice;
        if (invoice?.SubscriptionId is null) return;

        var sub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == invoice.SubscriptionId, ct);
        if (sub is null) return;

        sub.Status = SubscriptionStatus.PastDue;
        sub.GracePeriodEndsAt = DateTimeOffset.UtcNow.AddDays(3);
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleSubscriptionUpdatedAsync(Event e, CancellationToken ct)
    {
        var stripeSub = e.Data.Object as Stripe.Subscription;
        if (stripeSub is null) return;

        var sub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id, ct);
        if (sub is null) return;

        sub.Status = MapStatus(stripeSub.Status);
        sub.CurrentPeriodEnd = stripeSub.CurrentPeriodEnd;
        sub.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task HandleSubscriptionDeletedAsync(Event e, CancellationToken ct)
    {
        var stripeSub = e.Data.Object as Stripe.Subscription;
        if (stripeSub is null) return;

        var sub = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSub.Id, ct);
        if (sub is null) return;

        sub.Status = SubscriptionStatus.Canceled;
        sub.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static SubscriptionStatus MapStatus(string stripeStatus) => stripeStatus switch
    {
        "active" => SubscriptionStatus.Active,
        "past_due" => SubscriptionStatus.PastDue,
        "canceled" => SubscriptionStatus.Canceled,
        "trialing" => SubscriptionStatus.Trialing,
        _ => SubscriptionStatus.Incomplete
    };
}
