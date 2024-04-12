using Microsoft.Extensions.Options;

namespace Minio;

public class MinioStaticCredentialsProvider : IMinioCredentialsProvider
{
    private readonly IOptions<StaticCredentialsOptions> _options;

    public MinioStaticCredentialsProvider(IOptions<StaticCredentialsOptions> options)
    {
        _options = options;
    }
    
    public ValueTask<Credentials> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        return new ValueTask<Credentials>(new Credentials(options.AccessKey, options.SecretKey));
    }
}