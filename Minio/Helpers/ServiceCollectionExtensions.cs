using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Minio.CredentialProviders;
using Minio.Implementation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public interface IMinioBuilder
{
    IMinioBuilder WithStaticCredentials(Action<StaticCredentialsOptions> configure);
    IMinioBuilder WithStaticCredentials(string accessKey, string secretKey, string? sessionToken = null);
    IMinioBuilder WithEnvironmentCredentials();
}

public static class ServiceCollectionServiceExtensions
{
    private sealed class MinioBuilder : IMinioBuilder
    {
        private readonly IServiceCollection _serviceCollection;

        public MinioBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        public IMinioBuilder WithStaticCredentials(Action<StaticCredentialsOptions>? configure = null)
        {
            if (configure != null)
                _serviceCollection.Configure(configure);
            _serviceCollection.AddSingleton<ICredentialsProvider, StaticCredentialsProvider>();
            return this;
        }
        
        public IMinioBuilder WithStaticCredentials(string accessKey, string secretKey, string? sessionToken = null)
            => WithStaticCredentials(opts =>
            {
                opts.AccessKey = accessKey;
                opts.SecretKey = secretKey;
                opts.SessionToken = sessionToken;
            });

        public IMinioBuilder WithEnvironmentCredentials()
        {
            _serviceCollection.AddSingleton<ICredentialsProvider, EnvironmentCredentialsProvider>();
            return this;
        }
    }
    
    public static IMinioBuilder AddMinio(
        this IServiceCollection services,
        Action<ClientOptions>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        services.AddHttpClient("MinioClient").AddPolicyHandler(MinioClientBuilder.GetRetryPolicy());
        services.TryAddSingleton<ITimeProvider, DefaultTimeProvider>();
        services.TryAdd(new ServiceDescriptor(typeof(IRequestAuthenticator), typeof(V4RequestAuthenticator), lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IMinioClient), typeof(MinioClient), lifetime));
        if (configure != null)
            services.Configure(configure);
        return new MinioBuilder(services);
    }

    public static IMinioBuilder AddMinio(this IServiceCollection services, Uri endPoint)
        => services.AddMinio(opts => opts.EndPoint = endPoint);

    public static IMinioBuilder AddMinio(this IServiceCollection services, string endPoint)
        => services.AddMinio(opts => opts.EndPoint = new Uri(endPoint));
}