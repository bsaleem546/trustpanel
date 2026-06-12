using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Common;
using TrustPanel.Application.Forms;
using TrustPanel.Domain.Forms;

namespace TrustPanel.Api.Endpoints;

public static class FormEndpoints
{
    public static void MapFormEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/forms").RequireAuthorization();

        group.MapGet("/", async (
            Guid? workspaceId, ClaimsPrincipal user, ICurrentWorkspace workspace, IMediator mediator) =>
        {
            var resolved = ResolveWorkspaceId(workspaceId, user, workspace);
            var forms = await mediator.Send(new ListFormsQuery(user.GetUserId(), resolved));
            return ApiResults.Ok(new { items = forms, total = forms.Count }, "Collection forms.");
        });

        group.MapPost("/", async (
            FormRequest request, Guid? workspaceId, ClaimsPrincipal user,
            ICurrentWorkspace workspace, IMediator mediator) =>
        {
            var resolved = ResolveWorkspaceId(workspaceId, user, workspace);
            var form = await mediator.Send(new CreateFormCommand(
                user.GetUserId(), resolved, request.ToPayload()));
            return ApiResults.Created(form, "Form created.");
        });

        group.MapGet("/{formId:guid}", async (Guid formId, ClaimsPrincipal user, IMediator mediator) =>
        {
            var form = await mediator.Send(new GetFormQuery(user.GetUserId(), formId));
            return ApiResults.Ok(form, "Form.");
        });

        group.MapPut("/{formId:guid}", async (
            Guid formId, FormRequest request, ClaimsPrincipal user, IMediator mediator) =>
        {
            var form = await mediator.Send(new UpdateFormCommand(
                user.GetUserId(), formId, request.ToPayload()));
            return ApiResults.Ok(form, "Form updated.");
        });

        group.MapDelete("/{formId:guid}", async (Guid formId, ClaimsPrincipal user, IMediator mediator) =>
        {
            await mediator.Send(new DeleteFormCommand(user.GetUserId(), formId));
            return ApiResults.NoContent("Form deleted.");
        });
    }

    private static Guid ResolveWorkspaceId(
        Guid? workspaceId, ClaimsPrincipal user, ICurrentWorkspace workspace)
        => workspaceId
            ?? workspace.WorkspaceId
            ?? user.GetWorkspaceId()
            ?? throw new NotFoundException("Workspace not found.");

    private sealed record FormRequest(
        string Name,
        SubmissionType? AllowedSubmissionType,
        QuestionConfigDto? QuestionConfig,
        ThankYouConfigDto? ThankYouConfig,
        RewardConfigDto? RewardConfig,
        bool? IsActive)
    {
        public FormConfigPayload ToPayload() => new(
            Name, AllowedSubmissionType, QuestionConfig, ThankYouConfig, RewardConfig, IsActive);
    }
}
