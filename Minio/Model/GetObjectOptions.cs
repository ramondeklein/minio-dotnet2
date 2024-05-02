namespace Minio.Model;

public class GetObjectOptions
{
    public IServerSideEncryption? ServerSideEncryption { get; set; }
    public string? IfMatchETag { get; set; }
    public string? IfMatchETagExcept { get; set; }
    public DateTimeOffset? IfUnmodifiedSince { get; set; }
    public DateTimeOffset? IfModifiedSince { get; set; }
    public S3Range? Range { get; set; }
    public string? VersionId { get; set; }
    public int? PartNumber { get; set; }
    public bool? CheckSum { get; set; }
}