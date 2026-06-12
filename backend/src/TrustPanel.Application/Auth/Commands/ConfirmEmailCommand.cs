using FluentValidation;
using MediatR;
using TrustPanel.Application.Common;

namespace TrustPanel.Application.Auth.Commands;

public sealed record ConfirmEmailCommand(Guid UserId, string Token) : IRequest;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Token).NotEmpty();
    }
}

public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand>
{
    private readonly IIdentityService _identityService;

    public ConfirmEmailCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ConfirmEmailAsync(request.UserId, request.Token);
        if (!result.Succeeded)
        {
            throw new UnauthorizedAppException("The email confirmation link is invalid or expired.");
        }
    }
}
