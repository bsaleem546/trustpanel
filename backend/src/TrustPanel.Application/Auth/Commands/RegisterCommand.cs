using FluentValidation;
using MediatR;
using TrustPanel.Application.Common;
using TrustPanel.Application.Workspaces;

namespace TrustPanel.Application.Auth.Commands;

public sealed record RegisterCommand(string Email, string Password, string? WorkspaceName)
    : IRequest<RegisterResult>;

public sealed record RegisterResult(Guid UserId, Guid WorkspaceId);

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(c => c.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(c => c.WorkspaceName).MaximumLength(128);
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IIdentityService _identityService;
    private readonly IAppDbContext _db;
    private readonly IAuthEmailSender _authEmailSender;

    public RegisterCommandHandler(
        IIdentityService identityService, IAppDbContext db, IAuthEmailSender authEmailSender)
    {
        _identityService = identityService;
        _db = db;
        _authEmailSender = authEmailSender;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var (result, userId) = await _identityService.CreateUserAsync(
            request.Email, request.Password, cancellationToken);

        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join(" ", result.Errors));
        }

        var (workspace, membership) = WorkspaceFactory.CreateDefault(
            userId, request.Email, request.WorkspaceName);
        _db.Workspaces.Add(workspace);
        _db.WorkspaceMembers.Add(membership);
        await _db.SaveChangesAsync(cancellationToken);

        var confirmationToken = await _identityService.GenerateEmailConfirmationTokenAsync(userId);
        await _authEmailSender.SendEmailConfirmationAsync(
            request.Email, userId, confirmationToken, cancellationToken);

        return new RegisterResult(userId, workspace.Id);
    }
}
