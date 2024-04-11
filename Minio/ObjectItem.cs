namespace Minio;

public class ObjectItem
{
    public required string Key { get; init; }
    public required string ETag { get; init; }
    public required long Size { get; init; }
    public required string StorageClass { get; init; }
}