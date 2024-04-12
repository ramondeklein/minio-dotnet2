using DotNet.Testcontainers.Builders;
using Testcontainers.Minio;
using Xunit;

namespace Minio.IntegrationTests.Tests;

public abstract class MinioTest : IAsyncLifetime
{
    private readonly MinioContainer _minioContainer;

    protected MinioTest()
    {
        _minioContainer = new MinioBuilder()
            .WithImage("quay.io/minio/minio:latest")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9000))
            .Build();
    }
        
    protected IMinioClient CreateClient()
    {
        return new MinioClientBuilder
            {
                EndPoint = new Uri(_minioContainer.GetConnectionString()),
                AccessKey = _minioContainer.GetAccessKey(),
                SecretKey = _minioContainer.GetSecretKey(),
            }
            .Build();        
    }
    
    public async Task InitializeAsync()
    {
        await _minioContainer.StartAsync().ConfigureAwait(true);
    }

    public async Task DisposeAsync()
    {
        await _minioContainer.StopAsync().ConfigureAwait(true);
    }
}