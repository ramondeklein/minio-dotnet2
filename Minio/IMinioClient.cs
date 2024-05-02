using Minio.Model;

namespace Minio;

public interface IMinioClient
{
    // Bucket operations
    IAsyncEnumerable<BucketInfo> ListBucketsAsync(CancellationToken cancellationToken = default);
    Task<bool> HeadBucketAsync(string bucketName, CancellationToken cancellationToken = default);
    Task<string> CreateBucketAsync(string bucketName, CreateBucketOptions? options = null, CancellationToken cancellationToken = default);
    Task DeleteBucketAsync(string bucketName, CancellationToken cancellationToken = default);
    
    // Object operations
    Task<CreateMultipartUploadResult> CreateMultipartUploadAsync(string bucketName, string objectName, CreateMultipartUploadOptions? options = null, CancellationToken cancellationToken = default);
    Task<UploadPartResult> UploadPartAsync(string bucketName, string objectName, string uploadId, int partNumber, Stream stream, UploadPartOptions? options = null, CancellationToken cancellationToken = default);
    Task<CompleteMultipartUploadResult> CompleteMultipartUploadAsync(string bucketName, string objectName, string uploadId, IEnumerable<PartInfo> parts, CompleteMultipartUploadOptions? options = null, CancellationToken cancellationToken = default);
    Task AbortMultipartUploadAsync(string bucketName, string objectName, string uploadId, CancellationToken cancellationToken = default);
    Task PutObjectAsync(string bucketName, string objectName, Stream stream, PutObjectOptions? options = null, CancellationToken cancellationToken = default);
    Task<ObjectInfo> HeadObjectAsync(string bucketName, string objectName, GetObjectOptions? options = null, CancellationToken cancellationToken = default);
    Task<(Stream, ObjectInfo)> GetObjectAsync(string bucketName, string objectName, GetObjectOptions? options = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ObjectItem> ListObjectsAsync(string bucketName, string? continuationToken = null, string? delimiter = null, string? encodingType = null, string? fetchOwner = null, int maxKeys = 0, string? prefix = null, string? startAfter = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<PartItem> ListPartsAsync(string bucketName, string objectName, string uploadId, int maxParts = 0, string? partNumberMarker = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<UploadItem> ListMultipartUploadsAsync(string bucketName, string? delimiter = null, string? encodingType = null, string? keyMarker = null, int maxUploads = 0, string? prefix = null, string? uploadIdMarker = null, CancellationToken cancellationToken = default);
}