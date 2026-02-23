using Microsoft.Extensions.Options;

namespace Minio.CredentialProviders;

public interface IAccessTokenProvider
{
    ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}

public class StaticAccessTokenProviderOptions
{
    public required string AccessToken { get; set; }
}

public class StaticAccessTokenProvider : IAccessTokenProvider
{
    private readonly IOptions<StaticAccessTokenProviderOptions> _options;

    public StaticAccessTokenProvider(IOptions<StaticAccessTokenProviderOptions> options)
    {
        _options = options;
    }
    
    public StaticAccessTokenProvider(string accessToken) : this(Options.Create(new StaticAccessTokenProviderOptions { AccessToken = accessToken }))
    {
    }
    
    public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<string>(_options.Value.AccessToken);
    }
}

public class EnvironmentAccessTokenProviderOptions
{
    public required string AccessTokenVariable { get; set; }
}

public class EnvironmentAccessTokenProvider : IAccessTokenProvider
{
    private readonly IOptions<EnvironmentAccessTokenProviderOptions> _options;

    public EnvironmentAccessTokenProvider(IOptions<EnvironmentAccessTokenProviderOptions> options)
    {
        _options = options;
    }
    
    public EnvironmentAccessTokenProvider(string accessTokenVariable) : this(Options.Create(new EnvironmentAccessTokenProviderOptions { AccessTokenVariable = accessTokenVariable }))
    {
    }
    
    public ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var accessToken = Environment.GetEnvironmentVariable(_options.Value.AccessTokenVariable) ?? string.Empty;
        return new ValueTask<string>(accessToken);
    }
}
public class FileAccessTokenProviderOptions
{
    public required string AccessTokenPath { get; set; }
}

public class FileAccessTokenProvider : IAccessTokenProvider
{
    private readonly IOptions<FileAccessTokenProviderOptions> _options;

    public FileAccessTokenProvider(IOptions<FileAccessTokenProviderOptions> options)
    {
        _options = options;
    }
    
    public FileAccessTokenProvider(string accessTokenPath) : this(Options.Create(new FileAccessTokenProviderOptions { AccessTokenPath = accessTokenPath }))
    {
    }
    
    public async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var accessToken = await File.ReadAllTextAsync(_options.Value.AccessTokenPath, cancellationToken).ConfigureAwait(false);
        return accessToken.Trim();
    }
}
