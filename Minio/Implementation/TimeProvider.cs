namespace Minio.Implementation;

public sealed class DefaultTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}