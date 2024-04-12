using Xunit;

namespace Minio.IntegrationTests.Tests;

public class MakeBucketTest : MinioTest
{
    [Fact]
    public async Task MakeStandardBucket()
    {
        var client = CreateClient();
        await client.MakeBucketAsync("test").ConfigureAwait(true);
        var bucketExists = await client.HeadBucketAsync("test").ConfigureAwait(true);
        Assert.True(bucketExists);
    }
}