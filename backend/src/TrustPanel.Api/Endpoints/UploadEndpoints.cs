using MediatR;
using System.Security.Claims;
using TrustPanel.Api.Responses;
using TrustPanel.Api.Security;
using TrustPanel.Application.Uploads;

namespace TrustPanel.Api.Endpoints;

public static class UploadEndpoints
{
    public static void MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/uploads").RequireAuthorization();

        // Request a pre-signed PUT URL for a video upload.
        // Client uploads directly to R2; object key is then submitted with the testimonial.
        group.MapPost("/video", async (
            VideoUploadRequest request,
            ClaimsPrincipal user,
            IMediator mediator,
            IConfiguration configuration) =>
        {
            var maxMb = int.TryParse(configuration["MAX_VIDEO_UPLOAD_MB"], out var mb) ? mb : 250;
            var result = await mediator.Send(new RequestVideoUploadCommand(
                request.ContentType, request.FileSizeBytes, maxMb));
            return ApiResults.Ok(result, "Pre-signed upload URL issued.");
        });

        // Get a short-lived read URL for a private object key.
        group.MapGet("/read-url", async (string objectKey, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVideoReadUrlQuery(objectKey));
            return ApiResults.Ok(result, "Pre-signed read URL issued.");
        });
    }

    private sealed record VideoUploadRequest(string ContentType, long FileSizeBytes);
}
