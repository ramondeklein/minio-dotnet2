namespace Minio.Model;

public readonly struct ObjectIdentifier
{    
    public required string Key { get; init; }
    public string? VersionId { get; init; }
    public string? ETag { get; init; }
    public DateTime? LastModifiedTime { get; init; }
    public long? Size { get; init; }

    public ObjectIdentifier(string key)
    {
        Key = key;
    }
}