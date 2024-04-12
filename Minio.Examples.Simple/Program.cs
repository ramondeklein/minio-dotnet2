using Minio;

var minioClient = new MinioClientBuilder
{
    EndPoint = new Uri("http://localhost:9000"),
    AccessKey = "minioadmin",
    SecretKey = "minioadmin",
}.Build();

// Create the test-bucket (if it doesn't exist)
const string testBucket = "testbucket";
var hasBucket = await minioClient.HeadBucketAsync(testBucket).ConfigureAwait(false);
if (!hasBucket)
    await minioClient.MakeBucketAsync(testBucket).ConfigureAwait(false);