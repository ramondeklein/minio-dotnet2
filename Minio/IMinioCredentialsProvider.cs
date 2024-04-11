namespace Minio;

public readonly record struct Credentials(string AccessKey, string SecretKey, string SessionToken = "");

public interface IMinioCredentialsProvider
{
    ValueTask<Credentials> GetCredentials(CancellationToken cancellationToken);
}