using Minio.Model;
using Minio.Model.Notification;

namespace Minio;

public static class MinioClientExtensions
{
    public static Task RemoveAllBucketNotificationsAsync(this IMinioClient client, string bucketName, CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        return client.SetBucketNotificationsAsync(bucketName, new BucketNotification(), cancellationToken);
    }

    public static Task<IAsyncEnumerable<BucketNotificationEvent>> ListenBucketNotificationsAsync(this IMinioClient client, string bucketName, EventType eventType, string prefix = "", string suffix = "", CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        return client.ListenBucketNotificationsAsync(bucketName, new[] { eventType }, prefix, suffix, cancellationToken);
    }
}
