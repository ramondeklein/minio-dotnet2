namespace Minio.Tests.Services;

public class StaticMinioCredentialsProvider : IMinioCredentialsProvider
{
    private readonly Credentials _credentials;

    public StaticMinioCredentialsProvider(string accessKey, string secretKey, string sessionToken = "")
    {
        _credentials = new Credentials(accessKey, secretKey, sessionToken);
    }
    public ValueTask<Credentials> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<Credentials>(_credentials);
    }
}