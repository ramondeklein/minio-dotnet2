using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio.Implementation;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Minio;

public sealed class MinioClientBuilder
{
    public required Uri EndPoint { get; init; }
    public string Region { get; set; } = "us-east-1";
    public string AccessKey { get; set; } = "minioadmin";  
    public string SecretKey { get; set; } = "minioadmin";

    public IMinioClient Build()
    {
        var clientOptions = Options.Create(new ClientOptions
        {
            EndPoint = EndPoint,
            Region = Region,
        });
        var credentialOptions = Options.Create(new StaticCredentialsOptions
        {
            AccessKey = AccessKey,
            SecretKey = SecretKey,
        });
        var timeProvider = new DefaultTimeProvider();
        var credentialsProvider = new MinioStaticCredentialsProvider(credentialOptions);
        var authLogger = NullLoggerFactory.Instance.CreateLogger<V4RequestAuthenticator>();
        var authenticator = new V4RequestAuthenticator(credentialsProvider, timeProvider, authLogger);
        var httpClientFactory = new HttpClientFactory();
        var minioLogger = NullLoggerFactory.Instance.CreateLogger<MinioClient>();
        return new MinioClient(clientOptions, timeProvider, authenticator, httpClientFactory, minioLogger);
    }
    
    internal static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(
                medianFirstRetryDelay: TimeSpan.FromMilliseconds(250),
                retryCount: 5
            ));
    }
    
    private sealed class HttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _httpMessageHandler;

        public HttpClientFactory()
        {
            var socketHandler = new SocketsHttpHandler();
            var pollyHttpMessageHandler = new PolicyHttpMessageHandler(GetRetryPolicy());
            pollyHttpMessageHandler.InnerHandler = socketHandler;
            _httpMessageHandler = pollyHttpMessageHandler;
        }

        public HttpClient CreateClient(string name) => new(_httpMessageHandler, false);
    }
}