using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

public class EventMeta
{
    [JsonPropertyName("bucket")]
    public BucketMeta Bucket { get; set; }

    [JsonPropertyName("configurationId")]
    public string ConfigurationId { get; set; }

    [JsonPropertyName("object")]
    public ObjectMeta Object { get; set; }

    [JsonPropertyName("s3SchemaVersion")]
    public string SchemaVersion { get; set; }
}