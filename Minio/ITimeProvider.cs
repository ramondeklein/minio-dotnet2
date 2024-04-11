namespace Minio;

public interface ITimeProvider
{
    public DateTime UtcNow { get; }
}