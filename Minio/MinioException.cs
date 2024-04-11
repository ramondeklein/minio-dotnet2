namespace Minio;

public class MinioException : Exception
{
    internal MinioException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}