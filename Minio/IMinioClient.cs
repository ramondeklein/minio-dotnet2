namespace Minio;

public interface IMinioClient
{
    Task<bool> HeadBucketAsync(string bucketName, CancellationToken cancellationToken = default);
    Task<string> MakeBucketAsync(string bucketName, string? location = null, bool objectLock = false, CancellationToken cancellationToken = default);
    Task PutObjectAsync(string bucketName, string key, Stream stream, string? contentType = null, CancellationToken cancellationToken = default);
    Task<Stream> GetObjectAsync(string bucketName, string key, string? versionId = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ObjectItem> ListObjectsAsync(string bucketName, string? prefix = null, string? delimiter = null, string? encodingType = null, string? startAfter = null, int maxKeys = 0, CancellationToken cancellationToken = default);
}