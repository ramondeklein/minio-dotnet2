using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

public class BucketNotificationEvent
{
    [JsonPropertyName("EventName")]
    public string EventName { get; set; }

    [JsonPropertyName("Key")]
    public string Key { get; set; }

    [JsonPropertyName("Records")]
    public IList<NotificationEvent> Records { get; set; } = new List<NotificationEvent>();
}
