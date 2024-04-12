namespace Minio;

public readonly record struct Credentials(string AccessKey, string SecretKey, string SessionToken = "");

public interface ICredentialsProvider
{
    ValueTask<Credentials> GetCredentialsAsync(CancellationToken cancellationToken);
}