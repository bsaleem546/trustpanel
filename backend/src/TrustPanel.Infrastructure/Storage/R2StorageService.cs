using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using TrustPanel.Application.Uploads;

namespace TrustPanel.Infrastructure.Storage;

public sealed class R2StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public R2StorageService(IAmazonS3 s3, IConfiguration configuration)
    {
        _s3 = s3;
        _bucket = configuration["R2_BUCKET_NAME"] ?? "trustpanel";
    }

    public async Task<PresignedUploadResult> CreateUploadUrlAsync(
        PresignedUploadRequest request, CancellationToken cancellationToken)
    {
        var putRequest = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = request.ObjectKey,
            Verb = HttpVerb.PUT,
            ContentType = request.ContentType,
            Expires = DateTime.UtcNow.Add(request.Expiry)
        };

        var url = await _s3.GetPreSignedURLAsync(putRequest);
        return new PresignedUploadResult(url, request.ObjectKey);
    }

    public async Task<PresignedReadResult> CreateReadUrlAsync(
        string objectKey, TimeSpan expiry, CancellationToken cancellationToken)
    {
        var getRequest = new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        };

        var url = await _s3.GetPreSignedURLAsync(getRequest);
        return new PresignedReadResult(url);
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        await _s3.DeleteObjectAsync(_bucket, objectKey, cancellationToken);
    }
}
