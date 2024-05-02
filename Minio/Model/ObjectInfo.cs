using System.Net.Http.Headers;

namespace Minio.Model;

public class ObjectInfo
{
    public required EntityTagHeaderValue Etag { get; init; }
    public required string Key { get; init; }
    public long? ContentLength { get; init; }
    public DateTimeOffset? LastModified { get; init; }
    public required MediaTypeHeaderValue ContentType { get; init; }
    public DateTimeOffset? Expires { get; init; }
    public string? VersionId { get; init; }
    public required bool IsDeleteMarker { get; init; }

    public string? ReplicationStatus { get; init; }

    public DateTimeOffset? Expiration { get; init; }

    public string? ExpirationRuleId { get; init; }
    //public string? StorageClass { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; }
    public IReadOnlyDictionary<string, string> UserMetadata { get; init; }
    public IReadOnlyDictionary<string, string> UserTags { get; init; }
    public int UserTagCount { get; init; }
    public Restore? Restore { get; init; }

    public string? ChecksumCRC32 { get; init; }
    public string? ChecksumCRC32C { get; init; }
    public string? ChecksumSHA1 { get; init; }
    public string? ChecksumSHA256 { get; init; }
}