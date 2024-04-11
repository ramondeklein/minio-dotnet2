using Microsoft.Extensions.DependencyInjection.Extensions;
using Minio;
using Minio.Implementation;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionServiceExtensions
{
    public static IServiceCollection AddMinio(
        this IServiceCollection services,
        Action<MinioClientOptions> configure,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        services.AddHttpClient();
        services.TryAddSingleton<ITimeProvider, DefaultTimeProvider>();
        services.TryAddSingleton<IMinioCredentialsProvider, MinioStaticCredentialsProvider>();
        services.TryAdd(new ServiceDescriptor(typeof(IRequestAuthenticator), typeof(V4RequestAuthenticator), lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IMinioClient), typeof(MinioClient), lifetime));
        services.Configure(configure);
        return services;
    }
}