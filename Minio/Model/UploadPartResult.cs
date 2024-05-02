namespace Minio.Model;

public class UploadPartResult
{
    public string? Etag { get; init; }
    public string? ChecksumCRC32 { get; init; }
    public string? ChecksumCRC32C { get; init; }
    public string? ChecksumSHA1 { get; init; }
    public string? ChecksumSHA256 { get; init; }
}