using Microsoft.Extensions.Options;

namespace Minio.CredentialProviders;

public class StaticCredentialsProvider : ICredentialsProvider
{
    private readonly IOptions<StaticCredentialsOptions> _options;

    public StaticCredentialsProvider(IOptions<StaticCredentialsOptions> options)
    {
        _options = options;
    }
    
    public ValueTask<Credentials?> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        var accessKey = options.AccessKey;
        var secretKey = options.SecretKey;
        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey)) 
            return new ValueTask<Credentials?>((Credentials?)null);
        return new ValueTask<Credentials?>(new Credentials(accessKey, secretKey));
    }
}