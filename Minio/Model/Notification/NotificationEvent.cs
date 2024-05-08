using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

public class NotificationEvent
{
    [JsonPropertyName("awsRegion")]
    public string AwsRegion { get; set; }

    [JsonPropertyName("eventName")]
    public string EventName { get; set; }

    [JsonPropertyName("eventSource")]
    public string EventSource { get; set; }

    [JsonPropertyName("eventTime")]
    public string EventTime { get; set; }

    [JsonPropertyName("eventVersion")]
    public string EventVersion { get; set; }

    [JsonPropertyName("requestParameters")]
    public IDictionary<string, string> RequestParameters { get; set; } = new Dictionary<string, string>();

    [JsonPropertyName("responseElements")]
    public IDictionary<string, string> ResponseElements { get; set; } = new Dictionary<string, string>();

    [JsonPropertyName("s3")]
    public EventMeta S3 { get; set; }

    [JsonPropertyName("source")]
    public SourceInfo Source { get; set; }

    [JsonPropertyName("userIdentity")]
    public Identity UserIdentity { get; set; }
}