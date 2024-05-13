namespace Minio.CredentialProviders;

public class StaticCredentialsOptions
{
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public string? SessionToken { get; set; }
}