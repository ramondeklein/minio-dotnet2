using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio.Implementation;
using Minio.Tests.Services;

namespace Minio.Tests.UnitTests;

public abstract class MinioUnitTests
{
    public string MinioEndPoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string CurrentTime { get; set; } = "20240411T153713Z";
    
    protected IMinioClient GetMinioClient(Action<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var options = Options.Create(new MinioClientOptions
        {
            EndPoint = new Uri(MinioEndPoint)
        });
        var credentialsProvider = new StaticMinioCredentialsProvider(AccessKey, SecretKey);
        var timeProvider = new StaticTimeProvider(CurrentTime);
        var authLogger = NullLoggerFactory.Instance.CreateLogger<V4RequestAuthenticator>();
        var authenticator = new V4RequestAuthenticator(credentialsProvider, timeProvider, authLogger);
        var httpClientFactory = new TestHttpClientFactory(handler);
        var logger = NullLoggerFactory.Instance.CreateLogger<MinioClient>();
        return new MinioClient(options, timeProvider, authenticator, httpClientFactory, logger);
    }
}