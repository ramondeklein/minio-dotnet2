using Minio.Model;
using Minio.Model.Notification;

namespace Minio;

public static class MinioClientExtensions
{
    public static Task RemoveAllBucketNotificationsAsync(this IMinioClient client, string bucketName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SetBucketNotificationsAsync(bucketName, new BucketNotification(), cancellationToken);
    }
    
    public static Task DeleteBucketTaggingAsync(this IMinioClient client, string bucketName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SetBucketTaggingAsync(bucketName, null, cancellationToken);
    }
    
    public static Task RemoveObjectLockConfigurationAsync(this IMinioClient client, string bucketName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.SetObjectLockConfigurationAsync(bucketName, null, cancellationToken);
    }

    public static Task<IObservable<NotificationEvent>> ListenBucketNotificationsAsync(this IMinioClient client, string bucketName, EventType eventType, string prefix = "", string suffix = "", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.ListenBucketNotificationsAsync(bucketName, [eventType], prefix, suffix, cancellationToken);
    }
}
