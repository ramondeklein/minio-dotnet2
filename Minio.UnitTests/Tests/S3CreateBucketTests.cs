using System.Net;
using Minio.Model;
using Minio.UnitTests.Helpers;
using Xunit;

namespace Minio.UnitTests.Tests;

public class S3CreateBucketUnitTests : MinioUnitTests
{
    [Fact]
    public async Task CheckCreateBucket()
    {
        var minioClient = GetMinioClient((req, resp) =>
        {
            // Check request
            Assert.Equal("PUT", req.Method.Method);
            Assert.Equal("http://localhost:9000/testbucket", req.RequestUri?.ToString());
            req.AssertHeaders(
                "host: localhost:9000",
                "authorization: AWS4-HMAC-SHA256 Credential=minioadmin/20240411/us-east-1/s3/aws4_request, SignedHeaders=host;x-amz-bucket-object-lock-enabled;x-amz-content-sha256;x-amz-date, Signature=c3303afe730a9e019dcc72952950a85bab07856f00f807f6ecebdff6d9de2ffa",
                "x-amz-bucket-object-lock-enabled: true",
                "x-amz-content-sha256: 2d3d7a733990ce859d7275c8c9485a13dcf69c33c971b8e34362223442e02273",
                "x-amz-date: 20240411T153713Z"
            );
            
            // Set response
            resp.Headers.Location = new Uri("/testbucket", UriKind.Relative);
            resp.StatusCode = HttpStatusCode.OK;
        });

        var opts = new CreateBucketOptions { Region = "us-east-2 ", ObjectLocking = true };
        var location = await minioClient.CreateBucketAsync("testbucket", opts).ConfigureAwait(true);
        
        // Check result
        Assert.Equal("/testbucket", location);
    }
}