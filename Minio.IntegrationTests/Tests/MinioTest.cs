using Testcontainers.Minio;
using Xunit;

namespace Minio.IntegrationTests.Tests;

public abstract class MinioTest : IAsyncLifetime
{
    private readonly MinioContainer _minioContainer = new MinioBuilder()
        .WithImage("quay.io/minio/minio:latest")
        .Build();

    public Task InitializeAsync() => _minioContainer.StartAsync();
    public Task DisposeAsync() => _minioContainer.StopAsync();

    protected IMinioClient CreateClient()
    {
        return new MinioClientBuilder(_minioContainer.GetConnectionString())
            .WithStaticCredentials(_minioContainer.GetAccessKey(), _minioContainer.GetSecretKey())
            .Build();        
    }
}