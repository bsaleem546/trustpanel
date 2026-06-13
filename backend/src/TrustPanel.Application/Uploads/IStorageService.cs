namespace TrustPanel.Application.Uploads;

public sealed record PresignedUploadRequest(
    string ObjectKey,
    string ContentType,
    long MaxBytes,
    TimeSpan Expiry);

public sealed record PresignedUploadResult(string UploadUrl, string ObjectKey);
public sealed record PresignedReadResult(string ReadUrl);

public interface IStorageService
{
    /// <summary>Issues a pre-signed PUT URL for direct client upload.</summary>
    Task<PresignedUploadResult> CreateUploadUrlAsync(
        PresignedUploadRequest request, CancellationToken cancellationToken);

    /// <summary>Issues a short-lived pre-signed GET URL for private object access.</summary>
    Task<PresignedReadResult> CreateReadUrlAsync(
        string objectKey, TimeSpan expiry, CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
