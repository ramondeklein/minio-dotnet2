using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

public class SourceInfo
{
    [JsonPropertyName("host")]
    public string Host { get; set; }

    [JsonPropertyName("port")]
    public string Port { get; set; }

    [JsonPropertyName("userAgent")]
    public string UserAgent { get; set; }
}