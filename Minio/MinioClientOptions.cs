namespace Minio;

public class MinioClientOptions
{
    public required Uri EndPoint { get; set; }
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Region { get; set; } = "us-east-1";
    public string MinioHttpClient { get; set; } = "Minio";
}