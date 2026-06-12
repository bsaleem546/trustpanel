using MediatR;

namespace TrustPanel.Application.Auth.Commands;

public sealed record LogoutCommand(string? RefreshToken) : IRequest;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly AuthSessionService _sessions;

    public LogoutCommandHandler(AuthSessionService sessions)
    {
        _sessions = sessions;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            await _sessions.RevokeByRawTokenAsync(request.RefreshToken, cancellationToken);
        }
    }
}

public sealed record RevokeSessionCommand(Guid UserId, Guid SessionId) : IRequest;

public sealed class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand>
{
    private readonly AuthSessionService _sessions;

    public RevokeSessionCommandHandler(AuthSessionService sessions)
    {
        _sessions = sessions;
    }

    public Task Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
        => _sessions.RevokeSessionAsync(request.UserId, request.SessionId, cancellationToken);
}

public sealed record ListSessionsQuery(Guid UserId, Guid? CurrentSessionId)
    : IRequest<IReadOnlyList<SessionDto>>;

public sealed class ListSessionsQueryHandler
    : IRequestHandler<ListSessionsQuery, IReadOnlyList<SessionDto>>
{
    private readonly AuthSessionService _sessions;

    public ListSessionsQueryHandler(AuthSessionService sessions)
    {
        _sessions = sessions;
    }

    public Task<IReadOnlyList<SessionDto>> Handle(
        ListSessionsQuery request, CancellationToken cancellationToken)
        => _sessions.ListSessionsAsync(request.UserId, request.CurrentSessionId, cancellationToken);
}
