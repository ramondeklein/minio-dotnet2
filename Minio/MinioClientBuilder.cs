using System.Net;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio.CredentialProviders;
using Minio.Implementation;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Retry;

namespace Minio;

public sealed class MinioClientBuilder
{
    public Uri EndPoint { get; }
    public string Region { get; private set; } = "us-east-1";
    public ICredentialsProvider? CredentialsProvider { get; private set; }

    public MinioClientBuilder(string endPoint) : this(new Uri(endPoint))
    {
    }

    public MinioClientBuilder(Uri endPoint)
    {
        EndPoint = endPoint;
    }

    public IMinioClient Build()
    {
        if (CredentialsProvider == null)
            throw new InvalidOperationException("No credentials specified");

        var clientOptions = Options.Create(new ClientOptions
        {
            EndPoint = EndPoint,
            Region = Region,
        });
        var timeProvider = new DefaultTimeProvider();
        var authLogger = NullLoggerFactory.Instance.CreateLogger<V4RequestAuthenticator>();
        var authenticator = new V4RequestAuthenticator(CredentialsProvider, timeProvider, authLogger);
        var httpClientFactory = new HttpClientFactory();
        var minioLogger = NullLoggerFactory.Instance.CreateLogger<MinioClient>();
        return new MinioClient(clientOptions, timeProvider, authenticator, httpClientFactory, minioLogger);
    }

    public MinioClientBuilder WithRegion(string region)
    {
        Region = region;
        return this;
    }

    public MinioClientBuilder WithCredentialsProvider(ICredentialsProvider credentialsProvider)
    {
        CredentialsProvider = credentialsProvider;
        return this;
    }
    
    public MinioClientBuilder WithStaticCredentials(string accessKey, string secretKey, string? sessionToken = null)
    {
        var credentialOptions = Options.Create(new StaticCredentialsOptions
        {
            AccessKey = accessKey,
            SecretKey = secretKey,
            SessionToken = sessionToken,
        });
        return WithCredentialsProvider(new StaticCredentialsProvider(credentialOptions));
    }

    public MinioClientBuilder WithEnvironmentCredentials()
    {
        return WithCredentialsProvider(new EnvironmentCredentialsProvider());
    }
    
    internal static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return Policy<HttpResponseMessage>.Handle<HttpRequestException>().OrResult(resp =>
        {
            switch (resp.StatusCode)
            {
                case HttpStatusCode.RequestTimeout /* 408 */:
                case HttpStatusCode.Locked /* 423 */:
                case HttpStatusCode.TooManyRequests /* 429 */:
                case HttpStatusCode.InternalServerError /* 500 */:
                case HttpStatusCode.BadGateway /* 502 */:
                case HttpStatusCode.ServiceUnavailable /* 503 */:
                case HttpStatusCode.GatewayTimeout /* 504 */: 
                    return true;
            }
            return false;
        }).WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(
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