using FluentValidation;
using MediatR;
using TrustPanel.Application.Common;

namespace TrustPanel.Application.Auth.Commands;

public sealed record ForgotPasswordCommand(string Email) : IRequest;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IAuthEmailSender _authEmailSender;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService, IAuthEmailSender authEmailSender)
    {
        _identityService = identityService;
        _authEmailSender = authEmailSender;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Always succeeds from the caller's perspective so account existence is not revealed.
        var token = await _identityService.GeneratePasswordResetTokenAsync(request.Email);
        if (token is not null)
        {
            await _authEmailSender.SendPasswordResetAsync(request.Email, token, cancellationToken);
        }
    }
}

public sealed record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.Token).NotEmpty();
        RuleFor(c => c.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IIdentityService _identityService;
    private readonly AuthSessionService _sessions;

    public ResetPasswordCommandHandler(IIdentityService identityService, AuthSessionService sessions)
    {
        _identityService = identityService;
        _sessions = sessions;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ResetPasswordAsync(
            request.Email, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            throw new UnauthorizedAppException("The password reset link is invalid or expired.");
        }

        var user = await _identityService.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            await _sessions.RevokeAllSessionsAsync(user.Id, cancellationToken);
        }
    }
}
