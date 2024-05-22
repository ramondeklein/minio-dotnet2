using Minio.Helpers;
using Xunit;

namespace Minio.UnitTests.Tests;

public class VerificationTests
{
    public static readonly IEnumerable<object[]> BucketNames = new[]
    {
        new object[] {"docexamplebucket1", true},
        new object[] {"log-delivery-march-2020", true},
        new object[] {"my-hosted-content", true},
        new object[] {"docexamplewebsite.com", true},
        new object[] {"www.docexamplewebsite.com", true},
        new object[] {"my.example.s3.bucket", true},
        
        new object[] {"doc_example_bucket", false},
        new object[] {"DocExampleBucket", false},
        new object[] {"doc-example-bucket-", false},
    };

    [Theory]
    [MemberData(nameof(BucketNames))]
    public void CheckBucketNameValidation(string bucketName, bool valid)
    {
        if (valid)
            Assert.True(VerificationHelpers.VerifyBucketName(bucketName));
        else
            Assert.False(VerificationHelpers.VerifyBucketName(bucketName));
    }

    [Fact]
    public async Task CheckBucketException()
    {
        var minioClient = new MinioClientBuilder("http://localhost:9000")
            .WithStaticCredentials("minioadmin", "minioadmin")
            .Build();
        var exc = await Assert.ThrowsAsync<ArgumentException>(() => minioClient.CreateBucketAsync("-invalid-")).ConfigureAwait(true);
        Assert.Equal("bucketName", exc.ParamName);
    }
}