using System.Net.Http.Headers;

namespace Minio.Model;

public class CreateMultipartUploadOptions
{
    public CacheControlHeaderValue? CacheControl { get; set; }
    public ContentDispositionHeaderValue? ContentDisposition { get; set; }
    public ICollection<string>? ContentEncoding { get; set; }
    public ICollection<string>? ContentLanguage { get; set; }
    public MediaTypeHeaderValue? ContentType { get; set; }
    public DateTimeOffset? Expires { get; set; }

    public IServerSideEncryption? ServerSideEncryption { get; set; }

    public string? StorageClass { get; set; }
    public string? WebsiteRedirectLocation { get; set; }
    public IDictionary<string, string> UserTags { get; } = new Dictionary<string, string>();
    public RetentionMode? Mode { get; set; }
    public DateTimeOffset? RetainUntilDate { get; set; }
    public LegalHoldStatus? LegalHold { get; set; }
}