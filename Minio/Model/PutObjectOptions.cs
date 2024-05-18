using System.Net.Http.Headers;

namespace Minio.Model;

public class PutObjectOptions
{
    public IServerSideEncryption? ServerSideEncryption { get; set; }
    public string? IfMatchETag { get; set; }
    public string? IfMatchETagExcept { get; set; }
    public IDictionary<string, string> UserMetadata { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, string> UserTags { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public MediaTypeHeaderValue? ContentType { get; set; }
    public ICollection<string>? ContentEncoding { get; set; }
    public ContentDispositionHeaderValue? ContentDisposition { get; set; }
    public ICollection<string>? ContentLanguage { get; set; }
    public CacheControlHeaderValue? CacheControl { get; set; }
    public DateTimeOffset? Expires { get; set; }
    public RetentionMode? Mode { get; set; }
    public DateTimeOffset? RetainUntilDate { get; set; }
    public string? StorageClass { get; set; }
    public string? WebsiteRedirectLocation { get; set; }
    public LegalHoldStatus? LegalHold { get; set; }
}