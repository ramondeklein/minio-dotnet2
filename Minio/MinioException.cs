namespace Minio;

public class MinioException : Exception
{
    protected MinioException()
    {
    }
    
    protected MinioException(string message) : base(message)
    {
    }
    
    protected MinioException(string message, Exception innerException) : base(message, innerException)
    {
    }
}