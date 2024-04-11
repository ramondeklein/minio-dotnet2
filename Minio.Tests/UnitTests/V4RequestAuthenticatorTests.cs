using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Minio.Implementation;
using Minio.Tests.Services;
using Xunit;

namespace Minio.Tests.UnitTests;

public class V4RequestAuthenticatorTests
{
    [Fact]
    public async Task Validate_Authentication()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost:9000/test?delimiter=%2F&encoding-type=url&list-type=2&prefix=");
        req.Headers.Add("X-Amz-Content-Sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        req.Headers.Add("X-Amz-Date", "20240411T153713Z");

        var credsProvider = new StaticMinioCredentialsProvider("minioadmin", "minioadmin");
        var timeProvider = new StaticTimeProvider("20240411T153713Z");
        var logger = NullLoggerFactory.Instance.CreateLogger<V4RequestAuthenticator>();
        var authenticator = new V4RequestAuthenticator(credsProvider, timeProvider, logger);
        await authenticator.AuthenticateAsync(req, "us-east-1", "s3", default);

        Assert.NotNull(req.Headers.Authorization!.Scheme);
        Assert.Equal("AWS4-HMAC-SHA256", req.Headers.Authorization.Scheme);
        Assert.Equal("Credential=minioadmin/20240411/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature=fbc9b67904568217c4dcdd438483fa7ff914a793e532d215eecddae7f78bdfe8", req.Headers.Authorization.Parameter);
   }
}