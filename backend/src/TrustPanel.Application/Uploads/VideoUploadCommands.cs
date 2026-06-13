using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace TrustPanel.Application.Uploads;

public static class VideoUploadPolicy
{
    public static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/webm",
        "video/quicktime"
    };
}

public sealed record RequestVideoUploadCommand(
    string ContentType,
    long FileSizeBytes,
    int MaxUploadMb) : IRequest<PresignedUploadResult>;

public sealed class RequestVideoUploadCommandHandler
    : IRequestHandler<RequestVideoUploadCommand, PresignedUploadResult>
{
    private readonly IStorageService _storage;

    public RequestVideoUploadCommandHandler(IStorageService storage)
    {
        _storage = storage;
    }

    public async Task<PresignedUploadResult> Handle(
        RequestVideoUploadCommand request, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();

        if (!VideoUploadPolicy.AllowedMimeTypes.Contains(request.ContentType))
        {
            failures.Add(new ValidationFailure("contentType",
                $"Content type '{request.ContentType}' is not allowed. " +
                $"Allowed: {string.Join(", ", VideoUploadPolicy.AllowedMimeTypes)}"));
        }

        var maxBytes = (long)request.MaxUploadMb * 1024 * 1024;
        if (request.FileSizeBytes > maxBytes)
        {
            failures.Add(new ValidationFailure("fileSizeBytes",
                $"File exceeds maximum size of {request.MaxUploadMb} MB."));
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);

        var objectKey = $"videos/{Guid.NewGuid()}";
        return await _storage.CreateUploadUrlAsync(new PresignedUploadRequest(
            objectKey, request.ContentType, maxBytes, TimeSpan.FromMinutes(15)), cancellationToken);
    }
}

public sealed record GetVideoReadUrlQuery(string ObjectKey) : IRequest<PresignedReadResult>;

public sealed class GetVideoReadUrlQueryHandler
    : IRequestHandler<GetVideoReadUrlQuery, PresignedReadResult>
{
    private readonly IStorageService _storage;

    public GetVideoReadUrlQueryHandler(IStorageService storage)
    {
        _storage = storage;
    }

    public Task<PresignedReadResult> Handle(
        GetVideoReadUrlQuery request, CancellationToken cancellationToken)
        => _storage.CreateReadUrlAsync(request.ObjectKey, TimeSpan.FromMinutes(60), cancellationToken);
}
