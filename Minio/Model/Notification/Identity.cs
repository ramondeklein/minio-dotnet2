using System.Text.Json.Serialization;

namespace Minio.Model.Notification;

public class Identity
{
    [JsonPropertyName("principalId")] public string PrincipalId { get; set; }
}
