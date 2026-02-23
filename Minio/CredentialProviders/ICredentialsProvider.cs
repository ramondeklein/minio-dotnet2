namespace Minio.CredentialProviders;

public interface ICredentialsProvider
{
    ValueTask<Credentials> GetCredentialsAsync(CancellationToken cancellationToken);
}