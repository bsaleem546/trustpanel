using MediatR;

namespace TrustPanel.Application.Billing;

public sealed record CreateCheckoutSessionCommand(
    Guid UserId, string PriceId, string SuccessUrl, string CancelUrl)
    : IRequest<CheckoutSessionResult>;

public sealed class CreateCheckoutSessionCommandHandler
    : IRequestHandler<CreateCheckoutSessionCommand, CheckoutSessionResult>
{
    private readonly IBillingService _billing;

    public CreateCheckoutSessionCommandHandler(IBillingService billing)
    {
        _billing = billing;
    }

    public Task<CheckoutSessionResult> Handle(
        CreateCheckoutSessionCommand request, CancellationToken cancellationToken)
        => _billing.CreateCheckoutSessionAsync(
            request.UserId, request.PriceId, request.SuccessUrl, request.CancelUrl, cancellationToken);
}

public sealed record CreatePortalSessionCommand(Guid UserId, string ReturnUrl)
    : IRequest<PortalSessionResult>;

public sealed class CreatePortalSessionCommandHandler
    : IRequestHandler<CreatePortalSessionCommand, PortalSessionResult>
{
    private readonly IBillingService _billing;

    public CreatePortalSessionCommandHandler(IBillingService billing)
    {
        _billing = billing;
    }

    public Task<PortalSessionResult> Handle(
        CreatePortalSessionCommand request, CancellationToken cancellationToken)
        => _billing.CreatePortalSessionAsync(request.UserId, request.ReturnUrl, cancellationToken);
}
