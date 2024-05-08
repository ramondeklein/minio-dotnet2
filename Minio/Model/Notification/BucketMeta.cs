using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

public class BucketMeta
{
    [JsonPropertyName("arn")]
    public string Arn { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("ownerIdentity")]
    public Identity OwnerIdentity { get; set; }
}