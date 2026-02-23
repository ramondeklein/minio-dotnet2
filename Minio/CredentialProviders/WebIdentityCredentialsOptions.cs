namespace Minio.CredentialProviders;

public class WebIdentityCredentialsOptions
{
    public required string StsEndPoint { get; set; }
    public int DurationSeconds { get; set; } = 3600;
    public string Policy { get; set; }
    public string RoleARN { get; set; }
    public string TokenRevokeType { get; set; }
    public string MinioHttpClient { get; set; } = "Minio";
}