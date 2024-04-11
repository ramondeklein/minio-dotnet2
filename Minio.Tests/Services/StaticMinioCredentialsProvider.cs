namespace Minio.Tests.Services;

public class StaticMinioCredentialsProvider : IMinioCredentialsProvider
{
    public Credentials Credentials { get; }

    public StaticMinioCredentialsProvider(string accessKey, string secretKey, string sessionToken = "")
    {
        Credentials = new Credentials(accessKey, secretKey, sessionToken);
    }
    public ValueTask<Credentials> GetCredentials(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<Credentials>(Credentials);
    }
}