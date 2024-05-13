namespace Minio.Model;

public record struct BucketInfo
{
    public DateTimeOffset CreationDate { get; init; }
    public string Name { get; init; }
}