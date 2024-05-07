using System.Net.Http.Headers;

namespace Minio;

public class PartItem
{
    public required string ETag { get; init; }
    public required DateTimeOffset LastModified { get; init; }
    public required int PartNumber { get; init; }
    public required long Size { get; init; }
    
    public string? ChecksumCRC32 { get; init; }
    public string? ChecksumCRC32C { get; init; }
    public string? ChecksumSHA1 { get; init; }
    public string? ChecksumSHA256 { get; init; }
}

public class ObjectItem
{
    public required string Key { get; init; }
    public required string ETag { get; init; }
    public required long Size { get; init; }
    public required string StorageClass { get; init; }
    public required DateTimeOffset LastModified { get; init; }
    
    // The following properties are only present when metadata
    // is requested during listing (MinIO specific feature)
    public MediaTypeHeaderValue? ContentType { get; init; }
    public DateTimeOffset? Expires { get; init; }

    public IReadOnlyDictionary<string, string> UserMetadata { get; init; }
}

public class UploadItem
{
    public required string Key { get; init; }
    public required string UploadId { get; init; }
    public required string StorageClass { get; init; }
    public required DateTimeOffset Initiated { get; init; }
}