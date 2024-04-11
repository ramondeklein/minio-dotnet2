namespace Minio;

public abstract class MinioException : Exception
{
    internal MinioException()
    {
    }
    
    internal MinioException(string message) : base(message)
    {
    }
    
    internal MinioException(string message, Exception innerException) : base(message, innerException)
    {
    }
}