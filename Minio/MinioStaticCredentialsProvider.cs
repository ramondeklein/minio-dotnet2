using Microsoft.Extensions.Options;

namespace Minio;

public class MinioStaticCredentialsProvider : IMinioCredentialsProvider
{
    private readonly IOptions<MinioClientOptions> _options;

    public MinioStaticCredentialsProvider(IOptions<MinioClientOptions> options)
    {
        _options = options;
    }
    
    public ValueTask<Credentials> GetCredentials(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        return new ValueTask<Credentials>(new Credentials(options.AccessKey, options.SecretKey));
    }
}