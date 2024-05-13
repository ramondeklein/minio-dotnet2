using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio.CredentialProviders;
using Minio.Implementation;
using Minio.UnitTests.Services;

namespace Minio.UnitTests.Tests;

public abstract class MinioUnitTests
{
    // ReSharper disable MemberCanBePrivate.Global
    protected string MinioEndPoint { get; set; } = "http://localhost:9000";
    protected string AccessKey { get; set; } = "minioadmin";
    protected string SecretKey { get; set; } = "minioadmin";
    protected string CurrentTime { get; set; } = "20240411T153713Z";
    // ReSharper restore MemberCanBePrivate.Global

    protected IMinioClient GetMinioClient(Action<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var options = Options.Create(new ClientOptions
        {
            EndPoint = new Uri(MinioEndPoint)
        });
        var credentialsProvider = new StaticCredentialsProvider(Options.Create(new StaticCredentialsOptions
        {
            AccessKey = AccessKey,
            SecretKey = SecretKey,
        }));
        var timeProvider = new StaticTimeProvider(CurrentTime);
        var authLogger = NullLoggerFactory.Instance.CreateLogger<V4RequestAuthenticator>();
        var authenticator = new V4RequestAuthenticator(credentialsProvider, timeProvider, authLogger);
        using var httpClientFactory = new TestHttpClientFactory(handler);
        var logger = NullLoggerFactory.Instance.CreateLogger<MinioClient>();
        return new MinioClient(options, timeProvider, authenticator, httpClientFactory, logger);
    }
}