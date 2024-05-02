namespace Minio.Model;

public class CompleteMultipartUploadResult
{
    public required string Location { get; init; }
    public required string Bucket { get; init; }
    public required string Key { get; init; }
    public required string Etag { get; init; }
    public string? ChecksumCRC32 { get; init; }
    public string? ChecksumCRC32C { get; init; }
    public string? ChecksumSHA1 { get; init; }
    public string? ChecksumSHA256 { get; init; }
}