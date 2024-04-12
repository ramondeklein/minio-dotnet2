﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio.Implementation;
using Minio.UnitTests.Services;
using Xunit;

namespace Minio.UnitTests.Tests;

public class V4RequestAuthenticatorTests
{
    [Fact]
    public async Task ValidateAuthentication()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost:9000/test?delimiter=%2F&encoding-type=url&list-type=2&prefix=");
        req.Headers.Add("X-Amz-Content-Sha256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        req.Headers.Add("X-Amz-Date", "20240411T153713Z");

        var credsProvider = new StaticCredentialsProvider(Options.Create(new StaticCredentialsOptions
        {
            AccessKey = "minioadmin", 
            SecretKey = "minioadmin",
        }));
        var timeProvider = new StaticTimeProvider("20240411T153713Z");
        var logger = NullLoggerFactory.Instance.CreateLogger<V4RequestAuthenticator>();
        var authenticator = new V4RequestAuthenticator(credsProvider, timeProvider, logger);
        await authenticator.AuthenticateAsync(req, "us-east-1", "s3", default).ConfigureAwait(true);

        Assert.NotNull(req.Headers.Authorization!.Scheme);
        Assert.Equal("AWS4-HMAC-SHA256", req.Headers.Authorization.Scheme);
        Assert.Equal("Credential=minioadmin/20240411/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-content-sha256;x-amz-date, Signature=fbc9b67904568217c4dcdd438483fa7ff914a793e532d215eecddae7f78bdfe8", req.Headers.Authorization.Parameter);
   }
}