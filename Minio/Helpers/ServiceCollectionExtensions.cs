using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Minio.Implementation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public interface IMinioBuilder
{
    IMinioBuilder WithStaticCredentials(Action<StaticCredentialsOptions> configure);
    IMinioBuilder WithStaticCredentials(string accessKey, string secretKey);
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

        public IMinioBuilder WithStaticCredentials(Action<StaticCredentialsOptions> configure)
        {
            _serviceCollection.Configure(configure);
            _serviceCollection.AddSingleton<IMinioCredentialsProvider, MinioStaticCredentialsProvider>();
            return this;
        }
        
        public IMinioBuilder WithStaticCredentials(string accessKey, string secretKey)
            => WithStaticCredentials(opts =>
            {
                opts.AccessKey = accessKey;
                opts.SecretKey = secretKey;
            });
    }
    
    public static IMinioBuilder AddMinio(
        this IServiceCollection services,
        Action<ClientOptions> configure,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        services.AddHttpClient("MinioClient").AddPolicyHandler(MinioClientBuilder.GetRetryPolicy());
        services.TryAddSingleton<ITimeProvider, DefaultTimeProvider>();
        services.TryAddSingleton<IMinioCredentialsProvider, MinioStaticCredentialsProvider>();
        services.TryAdd(new ServiceDescriptor(typeof(IRequestAuthenticator), typeof(V4RequestAuthenticator), lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IMinioClient), typeof(MinioClient), lifetime));
        services.Configure(configure);
        return new MinioBuilder(services);
    }

    public static IMinioBuilder AddMinio(this IServiceCollection services, Uri endPoint)
        => services.AddMinio(opts => opts.EndPoint = endPoint);

    public static IMinioBuilder AddMinio(this IServiceCollection services, string endPoint)
        => services.AddMinio(opts => opts.EndPoint = new Uri(endPoint));
}