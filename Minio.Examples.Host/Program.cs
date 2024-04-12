using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Minio;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging
    .AddSimpleConsole(opt => opt.SingleLine = true) 
    .SetMinimumLevel(LogLevel.Warning);

// Add Minio
builder.Services.AddMinio("http://localhost:9000")
    .WithStaticCredentials("minioadmin", "minioadmin");

// Obtain a host
using var host = builder.Build();

// Obtain a Minio client
var minioClient = host.Services.GetRequiredService<IMinioClient>();

// Create the test-bucket (if it doesn't exist)
const string testBucket = "testbucket";
var hasBucket = await minioClient.HeadBucketAsync(testBucket).ConfigureAwait(false);
if (!hasBucket)
    await minioClient.MakeBucketAsync(testBucket).ConfigureAwait(false);

// Write out 100 objects in parallel
var buffer = new byte[256];
for (var i = 0; i < buffer.Length; ++i)
    buffer[i] = (byte)i;

await Task.WhenAll(Enumerable.Range(0, 100).Select(i => $"test-{i:D04}").Select(async key =>
{
    var ms = new MemoryStream(buffer, false);
    await using (ms.ConfigureAwait(false))
    {
        await minioClient.PutObjectAsync(testBucket, key, ms).ConfigureAwait(false);
    }
})).ConfigureAwait(false);

// Read an object file
var stream = await minioClient.GetObjectAsync(testBucket, "test-0000").ConfigureAwait(false);
await using (stream.ConfigureAwait(false))
{
    // TODO: Do something with the stream
    _ = stream;
}

// List all objects starting with "test-" in the test-bucket
// (max 20 objects at a time)
await foreach (var objItem in minioClient.ListObjectsAsync(testBucket, prefix: "test-", delimiter: "/", maxKeys: 20, encodingType: "url"))
    Console.WriteLine($"{objItem.Key,-40} {objItem.Size,10} bytes, etag: {objItem.ETag}");
