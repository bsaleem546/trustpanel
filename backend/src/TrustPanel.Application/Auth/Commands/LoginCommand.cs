using FluentValidation;
using MediatR;
using TrustPanel.Application.Common;

namespace TrustPanel.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password, string UserAgent, string IpAddress)
    : IRequest<AuthResultDto>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IIdentityService _identityService;
    private readonly AuthSessionService _sessions;

    public LoginCommandHandler(IIdentityService identityService, AuthSessionService sessions)
    {
        _identityService = identityService;
        _sessions = sessions;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityService.ValidateCredentialsAsync(request.Email, request.Password)
            ?? throw new UnauthorizedAppException("Invalid email or password.");

        if (!user.EmailConfirmed)
        {
            throw new UnauthorizedAppException("Please confirm your email address before signing in.");
        }

        return await _sessions.IssueAsync(user, request.UserAgent, request.IpAddress, cancellationToken);
    }
}
