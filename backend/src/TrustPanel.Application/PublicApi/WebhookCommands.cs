using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;
using TrustPanel.Domain.Integrations;

namespace TrustPanel.Application.PublicApi;

public sealed record WebhookEndpointDto(Guid Id, string Url, bool IsActive, DateTimeOffset CreatedAt);

public sealed record CreateWebhookEndpointCommand(Guid UserId, Guid WorkspaceId, string Url)
    : IRequest<WebhookEndpointDto>;

public sealed class CreateWebhookEndpointCommandHandler
    : IRequestHandler<CreateWebhookEndpointCommand, WebhookEndpointDto>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public CreateWebhookEndpointCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task<WebhookEndpointDto> Handle(
        CreateWebhookEndpointCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);

        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var endpoint = new WebhookEndpoint
        {
            WorkspaceId = request.WorkspaceId,
            Url = request.Url,
            Secret = secret
        };
        _db.WebhookEndpoints.Add(endpoint);
        await _db.SaveChangesAsync(cancellationToken);

        return new WebhookEndpointDto(endpoint.Id, endpoint.Url, endpoint.IsActive, endpoint.CreatedAt);
    }
}

public sealed record DeleteWebhookEndpointCommand(Guid UserId, Guid WorkspaceId, Guid EndpointId) : IRequest;

public sealed class DeleteWebhookEndpointCommandHandler : IRequestHandler<DeleteWebhookEndpointCommand>
{
    private readonly IAppDbContext _db;
    private readonly WorkspaceAccess _access;

    public DeleteWebhookEndpointCommandHandler(IAppDbContext db, WorkspaceAccess access)
    {
        _db = db; _access = access;
    }

    public async Task Handle(DeleteWebhookEndpointCommand request, CancellationToken cancellationToken)
    {
        await _access.RequireManageAsync(request.WorkspaceId, request.UserId, cancellationToken);
        var endpoint = await _db.WebhookEndpoints
            .FirstOrDefaultAsync(e => e.Id == request.EndpointId
                                   && e.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException("Webhook endpoint not found.");
        _db.WebhookEndpoints.Remove(endpoint);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
