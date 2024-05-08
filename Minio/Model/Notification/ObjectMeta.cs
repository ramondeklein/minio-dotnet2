using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

public class ObjectMeta
{
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }

    [JsonPropertyName("eTag")]
    public string Etag { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("sequencer")]
    public string Sequencer { get; set; }

    [JsonPropertyName("size")]
    public ulong Size { get; set; }

    [JsonPropertyName("userMetadata")]
    public IDictionary<string, string> UserMetadata { get; set; } = new Dictionary<string, string>();

    [JsonPropertyName("versionId")]
    public string VersionId { get; set; }
}