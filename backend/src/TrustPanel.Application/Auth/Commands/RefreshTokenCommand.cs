using FluentValidation;
using MediatR;

namespace TrustPanel.Application.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken, string UserAgent, string IpAddress)
    : IRequest<AuthResultDto>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly AuthSessionService _sessions;
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(AuthSessionService sessions, IIdentityService identityService)
    {
        _sessions = sessions;
        _identityService = identityService;
    }

    public Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return _sessions.RotateAsync(
            request.RefreshToken,
            _identityService.FindByIdAsync,
            request.UserAgent,
            request.IpAddress,
            cancellationToken);
    }
}
