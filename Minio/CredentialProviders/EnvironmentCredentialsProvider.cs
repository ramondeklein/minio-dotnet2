namespace Minio.CredentialProviders;

public class EnvironmentCredentialsProvider : ICredentialsProvider
{
    public ValueTask<Credentials> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        var accessKey = GetEnvironmentString("MINIO_ROOT_USER", "MINIO_ACCESS_KEY", "AWS_ACCESS_KEY_ID");
        var secretKey = GetEnvironmentString("MINIO_ROOT_PASSWORD", "MINIO_SECRET_KEY", "AWS_SECRET_ACCESS_KEY");
        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey)) throw new InvalidOperationException("No access key or secret key");
        var sessionToken = GetEnvironmentString("AWS_SESSION_TOKEN") ?? string.Empty;
        return new ValueTask<Credentials>(new Credentials(accessKey, secretKey, sessionToken));
    }

    private static string? GetEnvironmentString(params string[] variables)
    {
        foreach (var variable in variables)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrEmpty(value)) return value;
        }
        return null;
    }
}