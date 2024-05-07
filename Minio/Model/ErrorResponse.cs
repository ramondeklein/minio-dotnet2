namespace Minio.Model;

public class ErrorResponse
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public required string BucketName { get; init; }
    public required string Key { get; init; }
    public required string Resource { get; init; }
    public required string RequestId { get; init; }
    public required string HostId { get; init; }
    public required string Region { get; init; }
    public required string Server { get; init; }
}