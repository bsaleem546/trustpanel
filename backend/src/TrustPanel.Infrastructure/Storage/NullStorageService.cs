using TrustPanel.Application.Uploads;

namespace TrustPanel.Infrastructure.Storage;

/// <summary>Storage stub used when R2 credentials are not configured.</summary>
public sealed class NullStorageService : IStorageService
{
    public Task<PresignedUploadResult> CreateUploadUrlAsync(
        PresignedUploadRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new PresignedUploadResult($"https://null-storage/{request.ObjectKey}", request.ObjectKey));

    public Task<PresignedReadResult> CreateReadUrlAsync(
        string objectKey, TimeSpan expiry, CancellationToken cancellationToken)
        => Task.FromResult(new PresignedReadResult($"https://null-storage/{objectKey}"));

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
