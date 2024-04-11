namespace Minio;

internal interface ITimeProvider
{
    public DateTime UtcNow { get; }
}