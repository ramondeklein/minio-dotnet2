namespace Minio.Model;

public class CreateMultipartUploadResult
{
    public required string Bucket { get; init; }
    public required string Key { get; init; }
    public required string UploadId { get; init; }
    public DateTimeOffset? AbortDate { get; init; }
    public string? AbortRuleId { get; init; }
    public CreateMultipartUploadOptions? CreateOptions { get; init; }
}