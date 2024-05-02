namespace Minio.Model;

public struct BucketInfo
{
    public DateTimeOffset CreationDate { get; init; }
    public string Name { get; init; }
}