namespace Minio.Implementation;

internal sealed class DefaultTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}