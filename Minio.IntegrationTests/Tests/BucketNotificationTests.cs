using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Minio.Model;
using Minio.Model.Notification;
using NATS.Client.Core;
using Testcontainers.Minio;
using Testcontainers.Nats;
using Xunit;

namespace Minio.IntegrationTests.Tests;

public class BucketNotificationTests
{
    private class BucketNotificationEventDeserializer : INatsDeserialize<BucketNotificationEvent>
    {
        public BucketNotificationEvent? Deserialize(in ReadOnlySequence<byte> buffer)
            => JsonSerializer.Deserialize<BucketNotificationEvent>(buffer.FirstSpan);
    }

    [Fact]
    public async Task TestBucketNotificationsViaNats()
    {
        // Start NATS container
        await using var natsContainer = new NatsBuilder()
            .WithImage("nats:2.10")
            .WithExposedPort(4222)
            .Build();
        await natsContainer.StartAsync().ConfigureAwait(true);

        const string natsIdentifier = "test";
        const string natsSubject = "test-subject";

        // Subscribe to NATS
        var nats = new NatsConnection(new NatsOpts { Url = natsContainer.GetConnectionString() });
        await using (nats.ConfigureAwait(true))
        {
            var tsc = new TaskCompletionSource<BucketNotificationEvent>();
            var natsObservable = nats.SubscribeAsync(subject: natsSubject, serializer: new BucketNotificationEventDeserializer()).ToObservable();
            using var natsSubscription = natsObservable.Subscribe(e => tsc.TrySetResult(e.Data!));

            // Start MinIO container with proper NATS configuration
            await using var minioContainer = new MinioBuilder()
                .WithImage("quay.io/minio/minio:latest")
                .WithEnvironment(new Dictionary<string, string>
                {
                    { $"MINIO_NOTIFY_NATS_ENABLE_{natsIdentifier}", "on" },
                    { $"MINIO_NOTIFY_NATS_ADDRESS_{natsIdentifier}", $"{natsContainer.IpAddress}:4222" },
                    { $"MINIO_NOTIFY_NATS_SUBJECT_{natsIdentifier}", natsSubject },
                })
                .Build();
            await minioContainer.StartAsync().ConfigureAwait(true);

            var client = new MinioClientBuilder(minioContainer.GetConnectionString())
                .WithStaticCredentials(minioContainer.GetAccessKey(), minioContainer.GetSecretKey())
                .Build();

            const string bucketName = "testbucket";
            const string key = "test-nats";
            const string contentType = "test/plain";

            await client.CreateBucketAsync(bucketName).ConfigureAwait(true);
            var bucketNotification = new BucketNotification
            {
                QueueConfigs =
                {
                    new QueueConfig
                    {
                        Queue = $"arn:minio:sqs::{natsIdentifier}:nats",
                        Events = { EventType.ObjectCreatedAll }
                    }
                }
            };
            await client.SetBucketNotificationsAsync(bucketName, bucketNotification).ConfigureAwait(true);

            // Write the object
            var testData = "Hello world"u8.ToArray();
            using var ms = new MemoryStream(testData, false);
            await client.PutObjectAsync(bucketName, key, ms, new PutObjectOptions
            {
                ContentType = new MediaTypeHeaderValue(contentType)
            }).ConfigureAwait(true);

            // Wait until the NATS notification comes in
            var bucketNotificationEvent = await tsc.Task.ConfigureAwait(true);

            // Check the result of the event
            // (also checks JSON deserialization)
            Assert.Equal(EventType.ObjectCreatedPut, bucketNotificationEvent.EventName);
            Assert.Equal($"{bucketName}/{key}", bucketNotificationEvent.Key);
            Assert.Single(bucketNotificationEvent.Records);
            var record = bucketNotificationEvent.Records[0];
            Assert.Equal("2.0", record.EventVersion);
            Assert.Equal("minio:s3", record.EventSource);
            Assert.Equal("", record.AwsRegion);
            Assert.Equal(EventType.ObjectCreatedPut, record.EventName);
            Assert.Equal(minioContainer.GetAccessKey(), record.UserIdentity.PrincipalId);
            Assert.Equal(minioContainer.GetAccessKey(), record.RequestParameters["principalId"]);
            Assert.Equal("", record.RequestParameters["region"]);
            Assert.True(IPAddress.TryParse(record.RequestParameters["sourceIPAddress"], out _));
            Assert.NotEmpty(record.ResponseElements["x-amz-id-2"]);
            Assert.NotEmpty(record.ResponseElements["x-amz-request-id"]);
            Assert.NotEmpty(record.ResponseElements["x-minio-deployment-id"]);
            Assert.Equal($"http://{minioContainer.IpAddress}:9000", record.ResponseElements["x-minio-origin-endpoint"]);
            Assert.Equal("1.0", record.S3.SchemaVersion);
            Assert.Equal("Config", record.S3.ConfigurationId);
            Assert.Equal(bucketName, record.S3.Bucket.Name);
            Assert.Equal(minioContainer.GetAccessKey(), record.S3.Bucket.OwnerIdentity.PrincipalId);
            Assert.Equal($"arn:aws:s3:::{bucketName}", record.S3.Bucket.Arn);
            Assert.Equal(key, record.S3.Object.Key);
            Assert.Equal((ulong)testData.Length, record.S3.Object.Size);
            Assert.Equal("3e25960a79dbc69b674cd4ec67a72c62", record.S3.Object.Etag);
            Assert.Equal(contentType, record.S3.Object.ContentType);
            Assert.Equal(contentType, record.S3.Object.UserMetadata["content-type"]);
            Assert.NotEmpty(record.S3.Object.Sequencer);
            Assert.True(IPAddress.TryParse(record.Source.Host, out _));

            // Verify the bucket notification
            var returnedBucketNotification = await client.GetBucketNotificationsAsync(bucketName).ConfigureAwait(true);
            Assert.True(XmlHelpers.DeepEqualsWithNormalization(bucketNotification.Serialize(), returnedBucketNotification.Serialize()));
        }
    }

    [Fact]
    public async Task TestMinioBucketNotifications()
    {
        // Start MinIO container
        await using var minioContainer = new MinioBuilder()
            .WithImage("quay.io/minio/minio:latest")
            .Build();
        await minioContainer.StartAsync().ConfigureAwait(true);

        var client = new MinioClientBuilder(minioContainer.GetConnectionString())
            .WithStaticCredentials(minioContainer.GetAccessKey(), minioContainer.GetSecretKey())
            .Build();

        const string bucketName = "testbucket";
        const string key = "test-events";
        const string contentType = "test/plain";

        await client.CreateBucketAsync(bucketName).ConfigureAwait(true);

        var tsc = new TaskCompletionSource<BucketNotificationEvent>();
        var bucketNotifications = await client.ListenBucketNotificationsAsync(bucketName, EventType.ObjectCreatedAll).ConfigureAwait(true); 
        using var subscription = bucketNotifications.ToObservable().Subscribe(e => tsc.TrySetResult(e));

        // Wensure the listen action is actually submitted
        await Task.Delay(500000).ConfigureAwait(true);

        // Write the object
        var testData = "Hello world"u8.ToArray();
        using var ms = new MemoryStream(testData, false);
        await client.PutObjectAsync(bucketName, key, ms, new PutObjectOptions
        {
            ContentType = new MediaTypeHeaderValue(contentType)
        }).ConfigureAwait(true);

        // Wait until the NATS notification comes in
        var bucketNotificationEvent = await tsc.Task.ConfigureAwait(true);

        // Check the result of the event
        // (also checks JSON deserialization)
        Assert.Equal(EventType.ObjectCreatedPut, bucketNotificationEvent.EventName);
        Assert.Equal($"{bucketName}/{key}", bucketNotificationEvent.Key);
        Assert.Single(bucketNotificationEvent.Records);
        var record = bucketNotificationEvent.Records[0];
        Assert.Equal("2.0", record.EventVersion);
        Assert.Equal("minio:s3", record.EventSource);
        Assert.Equal("", record.AwsRegion);
        Assert.Equal(EventType.ObjectCreatedPut, record.EventName);
        Assert.Equal(minioContainer.GetAccessKey(), record.UserIdentity.PrincipalId);
        Assert.Equal(minioContainer.GetAccessKey(), record.RequestParameters["principalId"]);
        Assert.Equal("", record.RequestParameters["region"]);
        Assert.True(IPAddress.TryParse(record.RequestParameters["sourceIPAddress"], out _));
        Assert.NotEmpty(record.ResponseElements["x-amz-id-2"]);
        Assert.NotEmpty(record.ResponseElements["x-amz-request-id"]);
        Assert.NotEmpty(record.ResponseElements["x-minio-deployment-id"]);
        Assert.Equal($"http://{minioContainer.IpAddress}:9000", record.ResponseElements["x-minio-origin-endpoint"]);
        Assert.Equal("1.0", record.S3.SchemaVersion);
        Assert.Equal("Config", record.S3.ConfigurationId);
        Assert.Equal(bucketName, record.S3.Bucket.Name);
        Assert.Equal(minioContainer.GetAccessKey(), record.S3.Bucket.OwnerIdentity.PrincipalId);
        Assert.Equal($"arn:aws:s3:::{bucketName}", record.S3.Bucket.Arn);
        Assert.Equal(key, record.S3.Object.Key);
        Assert.Equal((ulong)testData.Length, record.S3.Object.Size);
        Assert.Equal("3e25960a79dbc69b674cd4ec67a72c62", record.S3.Object.Etag);
        Assert.Equal(contentType, record.S3.Object.ContentType);
        Assert.Equal(contentType, record.S3.Object.UserMetadata["content-type"]);
        Assert.NotEmpty(record.S3.Object.Sequencer);
        Assert.True(IPAddress.TryParse(record.Source.Host, out _));
    }
}