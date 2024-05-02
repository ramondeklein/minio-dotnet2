using Xunit;

namespace Minio.IntegrationTests.Tests;

public class BucketTests : MinioTest
{
    [Fact]
    public async Task MakeStandardBucket()
    {
        var client = CreateClient();
        await client.CreateBucketAsync("test").ConfigureAwait(true);
        var bucketExists = await client.HeadBucketAsync("test").ConfigureAwait(true);
        Assert.True(bucketExists);
    }

    [Fact]
    public async Task ListBuckets()
    {
        var startTimeUtc = DateTimeOffset.Now.AddMinutes(-1); // allow some deviation

        var client = CreateClient();
        await client.CreateBucketAsync("test1").ConfigureAwait(true);
        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(true);
        await client.CreateBucketAsync("test2").ConfigureAwait(true);

        var endTimeUtc = DateTimeOffset.Now.AddMinutes(1); // allow some deviation
        
        var buckets = await client.ListBucketsAsync().ToListAsync().ConfigureAwait(true);
        buckets.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        
        Assert.Equal(2, buckets.Count);
        Assert.Equal("test1", buckets[0].Name);
        Assert.Equal("test2", buckets[1].Name);
        Assert.InRange(buckets[0].CreationDate, startTimeUtc, endTimeUtc);
        Assert.InRange(buckets[1].CreationDate, startTimeUtc, endTimeUtc);
        Assert.True(buckets[0].CreationDate < buckets[1].CreationDate);
    }
}