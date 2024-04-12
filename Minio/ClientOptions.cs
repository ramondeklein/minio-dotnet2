namespace Minio;

public class ClientOptions
{
    public required Uri EndPoint { get; set; }
    public string Region { get; set; } = "us-east-1";
    public string MinioHttpClient { get; set; } = "Minio";
}