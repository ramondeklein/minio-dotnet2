using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio.Helpers;
using Minio.Model;
using Minio.Model.Notification;

#if NET6_0
using ArgumentException = Shims.ArgumentException; 
using ArgumentNullException = Shims.ArgumentNullException; 
using SHA256 = Shims.SHA256; 
#endif

namespace Minio.Implementation;

internal class MinioClient : IMinioClient
{
    private static readonly XNamespace Ns = Constants.S3Ns;
    
    private const string EmptySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private static readonly Regex ExpirationRegex = new("expiry-date=\"(.*?)\", rule-id=\"(.*?)\"", RegexOptions.Compiled);
    private static readonly Regex RestoreRegex = new("ongoing-request=\"(.*?)\"(, expiry-date=\"(.*?)\")?", RegexOptions.Compiled);
    private static readonly string[] PreserveKeys = new[]
    {
        "Content-Type",
        "Cache-Control",
        "Content-Encoding",
        "Content-Language",
        "Content-Disposition",
        "X-Amz-Storage-Class",
        "X-Amz-Object-Lock-Mode",
        "X-Amz-Object-Lock-Retain-Until-Date",
        "X-Amz-Object-Lock-Legal-Hold",
        "X-Amz-Website-Redirect-Location",
        "X-Amz-Server-Side-Encryption",
        "X-Amz-Tagging-Count",
        "X-Amz-Meta-",
    };

    
    private const long MaxMultipartPutObjectSize = 5L * 1024 * 1024 * 1024 * 1024; // 5TiB
    private const long MinPartSize = 16 * 1024 * 1024;  // 16MiB

    private readonly IOptions<ClientOptions> _options;
    private readonly ITimeProvider _timeProvider;
    private readonly IRequestAuthenticator _requestAuthenticator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MinioClient> _logger;

    public MinioClient(IOptions<ClientOptions> options, ITimeProvider timeProvider, IRequestAuthenticator requestAuthenticator, IHttpClientFactory httpClientFactory, ILogger<MinioClient> logger)
    {
        _options = options;
        _timeProvider = timeProvider;
        _requestAuthenticator = requestAuthenticator;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async IAsyncEnumerable<BucketInfo> ListBucketsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var req = CreateRequest(HttpMethod.Get, string.Empty);
        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        var buckets = xResponse.Root?.Element(Ns + "Buckets");
        if (buckets != null)
        {
            foreach (var xContent in buckets.Elements(Ns + "Bucket"))
            {
                yield return new BucketInfo
                {
                    CreationDate = xContent.Element(Ns + "CreationDate")?.Value.ParseIsoTimestamp() ?? DateTimeOffset.UnixEpoch,
                    Name = xContent.Element(Ns + "Name")?.Value ?? string.Empty,
                };
            }
        }
    }

    public async Task<bool> HeadBucketAsync(string bucketName, CancellationToken cancellationToken)
    {
        try
        {
            using var req = CreateRequest(HttpMethod.Head, bucketName);
            await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (MinioHttpException exc) when (exc.Response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<string> CreateBucketAsync(string bucketName, CreateBucketOptions? options, CancellationToken cancellationToken)
    {
        var region = options?.Region;
        
        var xml = new XElement(Ns + "CreateBucketConfiguration");
        if (!string.IsNullOrEmpty(region) && region != "us-east-1")
        {
            xml.Add(new XElement(Ns + "Location",
                new XElement(Ns + "Name", region)));
        }

        using var req = CreateRequest(HttpMethod.Put, bucketName, xml);
        if (options?.ObjectLocking ?? false)
            req.Headers.Add("X-Amz-Bucket-Object-Lock-Enabled", "true");

        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
        return resp.GetHeaderValue("Location") ?? "";
    }

    public async Task DeleteBucketAsync(string bucketName, CancellationToken cancellationToken)
    {
        using var req = CreateRequest(HttpMethod.Delete, bucketName);
        await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CreateMultipartUploadResult> CreateMultipartUploadAsync(string bucketName, string key, CreateMultipartUploadOptions? options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        
        var query = new QueryParams();
        query.Add("uploads", string.Empty);
        
        using var req = CreateRequest(HttpMethod.Post, $"{bucketName}/{key}", query);

        req
            .SetContentType(options?.ContentType)
            .SetContentEncoding(options?.ContentEncoding)
            .SetContentDisposition(options?.ContentDisposition)
            .SetContentLanguage(options?.ContentLanguage)
            .SetCacheControl(options?.CacheControl)
            .SetExpires(options?.Expires)
            .SetObjectLockMode(options?.Mode)
            .SetObjectLockRetainUntilDate(options?.RetainUntilDate)
            .SetObjectLockLegalHold(options?.LegalHold)
            .SetStorageClass(options?.StorageClass)
            .SetWebsiteRedirectLocation(options?.WebsiteRedirectLocation)
            .SetTagging(options?.UserTags);

        options?.ServerSideEncryption?.WriteHeaders(req.Headers);
    
        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var abortDate = DateTimeOffset.TryParseExact(resp.Headers.TryGetValue("X-Amz-Abort-Date"), "R", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ad) ? (DateTimeOffset?)ad : null;
        var abortRuleId = resp.Headers.TryGetValue("X-Amz-Abort-Rule-Id");
        
        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        return new CreateMultipartUploadResult
        {
            Bucket = xResponse.Root?.Element(Ns + "Bucket")?.Value ?? bucketName,
            Key = xResponse.Root?.Element(Ns + "Key")?.Value ?? key,
            UploadId = xResponse.Root?.Element(Ns + "UploadId")?.Value ?? string.Empty,
            AbortDate = abortDate,
            AbortRuleId = abortRuleId,
            CreateOptions = options
        };
    }

    public async Task<UploadPartResult> UploadPartAsync(string bucketName, string key, string uploadId, int partNumber, Stream stream, UploadPartOptions? options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(uploadId);
        if (partNumber < 1) throw new ArgumentOutOfRangeException(nameof(partNumber), "Part numbers start at 1");

        var query = new QueryParams();
        query.Add("partNumber", partNumber.ToString(CultureInfo.InvariantCulture));
        query.Add("uploadId", uploadId);
        
        using var req = CreateRequest(HttpMethod.Put, $"{bucketName}/{key}", query);

        req.Content = new StreamContent(stream);
        req
            .SetContentMD5(options?.ContentMD5)
            .SetChecksum(options?.ChecksumAlgorithm, options?.Checksum);
    
        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        return new UploadPartResult
        {
            Etag = resp.Headers.TryGetValue("ETag"),
            ChecksumCRC32 = resp.Headers.TryGetValue("x-amz-checksum-crc32"),
            ChecksumCRC32C = resp.Headers.TryGetValue("x-amz-checksum-crc32c"),
            ChecksumSHA1 = resp.Headers.TryGetValue("x-amz-checksum-sha1"),
            ChecksumSHA256 = resp.Headers.TryGetValue("Checksumsha256"),
        };
    }

    public async Task<CompleteMultipartUploadResult> CompleteMultipartUploadAsync(string bucketName, string key, string uploadId, IEnumerable<PartInfo> parts, CompleteMultipartUploadOptions? options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(uploadId);
        
        var query = new QueryParams();
        query.Add("uploadId", uploadId);

        var xml = new XElement(Ns + "CompleteMultipartUploadResult");
        var partNumber = 1;
        foreach (var part in parts)
        {
            var xPart = new XElement(Ns + "Part",
                new XElement(Ns + "PartNumber", partNumber++),
                new XElement(Ns + "ETag", part.Etag));

            if (part.ChecksumAlgorithm != null && part.Checksum != null)
            {
                var (header, length) = part.ChecksumAlgorithm switch
                {
                    ChecksumAlgorithm.Crc32 => (Ns + "ChecksumCRC32", 32),
                    ChecksumAlgorithm.Crc32c => (Ns + "ChecksumCRC32C", 32),
                    ChecksumAlgorithm.Sha1 => (Ns + "ChecksumSHA1", 128),
                    ChecksumAlgorithm.Sha256 => (Ns + "ChecksumSHA256", 256),
                    _ => throw new System.ArgumentException("Invalid checksum algorithm", nameof(parts))
                };
                if (part.Checksum.Length * 8 != length)
                    throw new System.ArgumentException($"Expected {length}-bit checksum", nameof(parts));
                xPart.Add(new XElement(header, Convert.ToBase64String(part.Checksum)));
            }

            xml.Add(xPart);
        }

        using var req = CreateRequest(HttpMethod.Post, $"{bucketName}/{key}", xml, query);
        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        return new CompleteMultipartUploadResult
        {
            Location = xResponse.Root?.Element(Ns + "Location")?.Value ?? string.Empty,
            Bucket = xResponse.Root?.Element(Ns + "Bucket")?.Value ?? string.Empty,
            Key = xResponse.Root?.Element(Ns + "Key")?.Value ?? string.Empty,
            Etag = xResponse.Root?.Element(Ns + "ETag")?.Value ?? string.Empty,
            ChecksumCRC32 = xResponse.Root?.Element(Ns + "ChecksumCRC32")?.Value,
            ChecksumCRC32C = xResponse.Root?.Element(Ns + "ChecksumCRC32C")?.Value,
            ChecksumSHA1 = xResponse.Root?.Element(Ns + "ChecksumSHA1")?.Value,
            ChecksumSHA256 = xResponse.Root?.Element(Ns + "ChecksumSHA256")?.Value
        };
    }
    
    public async Task AbortMultipartUploadAsync(string bucketName, string key, string uploadId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(uploadId);

        var query = new QueryParams();
        query.Add("uploadId", uploadId);

        using var req = CreateRequest(HttpMethod.Delete, $"{bucketName}/{key}", query);
        await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task PutObjectAsync(string bucketName, string key, Stream stream, PutObjectOptions? options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(stream);

        var disableMultipart = false;
        //var disableMultipart = options?.DisableMultipart ?? false;
        if (disableMultipart && !stream.CanSeek)
            throw new System.ArgumentException("Stream length should be available with disable multipart upload", nameof(stream));
    
        if (stream.Length > MaxMultipartPutObjectSize)
            throw new System.ArgumentOutOfRangeException(nameof(stream), stream.Length, "Stream length out of range");
    
        if (IsGoogleEndpoint)
            disableMultipart = true;
        
        var partSize = options?.PartSize ?? MinPartSize;
    
        // if (!stream.CanSeek)
        // {
        //     var concurrentStreamParts = options?.ConcurrentStreamParts ?? false;
        //     var numThreads = options?.NumThreads ?? 0;
        //     if (concurrentStreamParts && numThreads > 1)
        //         await PutObjectMultipartStreamParallelAsync(bucketName, key, stream, options, cancellationToken).ConfigureAwait(false);
        //     else
        //         await PutObjectMultipartStreamNoLength(bucketName, key, stream, options, cancellationToken).ConfigureAwait(false);
        // }
        // else
        // {
        //     if (stream.Length < partSize)
                await PutObjectCoreAsync(bucketName, key, stream, options, cancellationToken).ConfigureAwait(false);
        //     else
        //         await PutObjectMultipartStream(bucketName, key, stream, options, cancellationToken).ConfigureAwait(false);
        // }
    }
    //
    // private async Task PutObjectMultipartStreamParallelAsync(string bucketName, string key, Stream stream, PutObjectOptions? options, CancellationToken cancellationToken)
    // {
    //     if (options?.SendContentMd5 ?? false)
    //         options.UserMetadata["X-Amz-Checksum-Algorithm"] = "CRC32C";
    //     
    //     var (totalPartsCount, partSize, _) = OptimalPartInfo(-1, options?.PartSize ?? 0);
    //     var uploadId = await NewUploadIdAsync(bucketName, key, options, cancellationToken).ConfigureAwait(false);
    //
    //     options?.UserMetadata.Remove("X-Amz-Checksum-Algorithm");
    //
    //     var crcBytes = new List<byte>(4 * totalPartsCount); // 32-bits per part
    //     
    //     try
    //     {
    //         var nBuffers = options?.NumThreads ?? 1;
    //         using var all = MemoryPool<byte>.Shared.Rent(nBuffers * partSize);
    //         var semaphore = new SemaphoreSlim(nBuffers);
    //         var bufs = new Stack<Memory<byte>>(nBuffers);
    //         for (var i = 0; i < nBuffers; ++i)
    //             bufs.Push(all.Memory[(i * partSize) .. partSize]);
    //
    //         // Part number always starts with '1'.
    //         for (var partNumber = 1; partNumber <= totalPartsCount; partNumber++)
    //         {
    //             await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
    //             var buf = bufs.Pop();
    //             try
    //             {
    //                 var read = await stream.ReadAsync(buf, cancellationToken).ConfigureAwait(false);
    //                 if (read != partSize)
    //                     throw new InvalidOperationException($"Expected to read {partSize} bytes, got only {read} bytes", );
    //
    //                 using var req = CreateRequest(HttpMethod.Put, "TODO");
    //
    //                 if (!(options?.SendContentMd5 ?? false))
    //                 {
    //                     var cSum = Crc32.Hash(buf.Span);
    //                     crcBytes.AddRange(cSum);
    //                     req.Headers.Add("X-Amz-Checksum-CRC32c", Convert.ToBase64String(cSum));
    //                 }
    //
    //                 string? md5base64 = null;
    //                 if (options?.SendContentMd5 ?? false)
    //                     md5base64 = Convert.ToBase64String(MD5.HashData(buf.Span));
    //
    //                 Task task = UploadPartAsync(bucketName, key, uploadId, buf.Span, partNumber, md5base64, read, options?.ServerSideEncryption ?? false, !(options?.DisableContentSha256 ?? false), customHeader, cancellationToken);
    //                 task.ContinueWith()
    //                 
    //                 {
    //                     
    //                 }
    //
    //                 //var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    //             }
    //             finally
    //             {
    //                 bufs.Push(buf);
    //                 semaphore.Release();
    //             }
    //         }
    //     }
    //     catch
    //     {
    //         await AbortMultipartUploadAsync(bucketName, key, uploadId, cancellationToken).ConfigureAwait(false);
    //         throw;
    //     }
    // }
    //
    private async Task PutObjectCoreAsync(string bucketName, string key, Stream stream, PutObjectOptions? options, CancellationToken cancellationToken)
    {
        using var req = CreateRequest(HttpMethod.Put, $"{bucketName}/{key}");
    
        req.Content = new StreamContent(stream);
        req
            .SetIfMatchETag(options?.IfMatchETag)
            .SetIfMatchETagExcept(options?.IfMatchETagExcept)
            .SetContentType(options?.ContentType)
            .SetContentEncoding(options?.ContentEncoding)
            .SetContentDisposition(options?.ContentDisposition)
            .SetContentLanguage(options?.ContentLanguage)
            .SetCacheControl(options?.CacheControl)
            .SetExpires(options?.Expires)
            .SetObjectLockMode(options?.Mode)
            .SetObjectLockRetainUntilDate(options?.RetainUntilDate)
            .SetObjectLockLegalHold(options?.LegalHold)
            .SetStorageClass(options?.StorageClass)
            .SetWebsiteRedirectLocation(options?.WebsiteRedirectLocation)
            .SetTagging(options?.UserTags)
            .SetUserMetadata(options?.UserMetadata);
            
        options?.ServerSideEncryption?.WriteHeaders(req.Headers);
    
        await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ObjectInfo> HeadObjectAsync(string bucketName, string key, GetObjectOptions? options = null, CancellationToken cancellationToken = default)
    {
        var resp = await GetOrHeadObjectAsync(HttpMethod.Head, bucketName, key, options, cancellationToken).ConfigureAwait(false);
        return ToObjectInfo(key, resp);
    }

    public async Task<(Stream, ObjectInfo)> GetObjectAsync(string bucketName, string key, GetObjectOptions? options, CancellationToken cancellationToken)
    {
        var resp = await GetOrHeadObjectAsync(HttpMethod.Get, bucketName, key, options, cancellationToken).ConfigureAwait(false);
        var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var objectInfo = ToObjectInfo(key, resp);
        return (stream, objectInfo);
    }

    private async Task<HttpResponseMessage> GetOrHeadObjectAsync(HttpMethod httpMethod, string bucketName, string key, GetObjectOptions? options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var q = new QueryParams();
        q.AddIfNotNullOrEmpty("versionId", options?.VersionId);
        if (options?.PartNumber != null)
            q.AddIfNotNullOrEmpty("partNumber", options.PartNumber.Value.ToString(CultureInfo.InvariantCulture));
        using var req = CreateRequest(httpMethod, $"{bucketName}/{key}", q);

        options?.ServerSideEncryption?.WriteHeaders(req.Headers);
        if (options?.CheckSum ?? false)
            req.Headers.Add("x-amz-checksum-mode", "ENABLED");
        if (options?.IfMatchETag != null)
        {
            if (string.IsNullOrEmpty(options.IfMatchETag)) throw new System.ArgumentException(nameof(options.IfMatchETag) + " should not be empty", nameof(options));
            req.Headers.Add("If-Match", '"' + options.IfMatchETag + '"');
        }
        if (options?.IfMatchETagExcept != null)
        {
            if (string.IsNullOrEmpty(options.IfMatchETagExcept)) throw new System.ArgumentException(nameof(options.IfMatchETagExcept) + " should not be empty", nameof(options));
            req.Headers.Add("If-None-Match", '"' + options.IfMatchETagExcept + '"');
        }
        req.Headers.AddIfNotNull("If-Unmodified-Since", options?.IfUnmodifiedSince?.ToIsoTimestamp());
        req.Headers.AddIfNotNull("If-Modified-Since", options?.IfModifiedSince?.ToIsoTimestamp());
        if (options?.Range != null)
        {
            var range = options.Range.Value;
            var rangeHeaderValue = range.Start switch
            {
                0 when range.End < 0 => $"bytes={range.End}",
                > 0 when range.End == 0 => $"bytes={range.Start}-",
                >= 0 when range.Start < range.End => $"bytes={range.Start}-{range.End}",
                _ => throw new System.ArgumentException("Invalid range", nameof(options))
            };
            req.Headers.Add("Range", rangeHeaderValue);
        }

        return await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<ObjectItem> ListObjectsAsync(string bucketName, string? continuationToken, string? delimiter, string? encodingType, bool includeMetadata, string? fetchOwner, int pageSize, string? prefix, string? startAfter, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);

        while (true)
        {
            var q = new QueryParams();
            q.Add("list-type", "2");
            q.AddIfNotNullOrEmpty("continuation-token", continuationToken);
            q.AddIfNotNullOrEmpty("delimiter", delimiter);
            q.AddIfNotNullOrEmpty("encoding-type", encodingType);
            if (includeMetadata)
                q.Add("metadata", "true");
            q.AddIfNotNullOrEmpty("fetch-owner", fetchOwner);
            if (pageSize > 0)
                q.Add("max-keys", pageSize.ToString(CultureInfo.InvariantCulture));
            q.AddIfNotNullOrEmpty("prefix", prefix);
            q.AddIfNotNullOrEmpty("start-after", startAfter);
            using var req = CreateRequest(HttpMethod.Get, bucketName, q);

            var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            foreach (var xContent in xResponse.Root!.Elements(Ns + "Contents"))
            {
                MediaTypeHeaderValue? contentType = null;
                DateTimeOffset? expires = null;
                var userMetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (includeMetadata)
                {
                    var xUserMetadata = xContent.Element(Ns + "UserMetadata");
                    if (xUserMetadata == null)
                        throw new InvalidOperationException("Client doesn't support metadata while listing objects (MinIO specific feature)");

                    contentType = MediaTypeHeaderValue.TryParse(xUserMetadata.Element(Ns + "content-type")?.Value, out var ct) ? ct : null;
                    expires = DateTimeOffset.TryParseExact(xUserMetadata.Element(Ns + "expires")?.Value, "r", CultureInfo.InvariantCulture, DateTimeStyles.None, out var v) ? v : null;
                    const string metaElementPrefix = "X-Amz-Meta-";
                    foreach (var xHeader in xUserMetadata.Elements().Where(x => x.Name.Namespace == Ns && x.Name.LocalName.StartsWith(metaElementPrefix, StringComparison.OrdinalIgnoreCase)))
                    {
                        var key = xHeader.Name.LocalName[metaElementPrefix.Length..];
                        userMetaData[key] = xHeader.Value;
                    }
                }

                var objItem = new ObjectItem
                {
                    Key = xContent.Element(Ns + "Key")?.Value ?? string.Empty,
                    ETag = xContent.Element(Ns + "ETag")?.Value ?? string.Empty,
                    Size = long.TryParse(xContent.Element(Ns + "Size")?.Value, out var size) ? size : -1,
                    StorageClass = xContent.Element(Ns + "Key")?.Value ?? string.Empty,
                    LastModified = xContent.Element(Ns + "LastModified")?.Value.ParseIsoTimestamp() ?? DateTimeOffset.MinValue,
                    ContentType = contentType,
                    Expires = expires,
                    UserMetadata = userMetaData
                };

                yield return objItem;
            }
            
            continuationToken = xResponse.Root!.Element(Ns + "NextContinuationToken")?.Value;
            var isTruncated = xResponse.Root!.Element(Ns + "IsTruncated")?.Value == "true";
            if (!isTruncated) break;
        }
    }

    public async IAsyncEnumerable<PartItem> ListPartsAsync(string bucketName, string key, string uploadId, int pageSize, string? partNumberMarker, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);

        while (true)
        {
            var q = new QueryParams();
            if (pageSize > 0)
                q.Add("max-parts", pageSize.ToString(CultureInfo.InvariantCulture));
            q.AddIfNotNullOrEmpty("part-number-marker", partNumberMarker);
            q.AddIfNotNullOrEmpty("uploadId", uploadId);
            using var req = CreateRequest(HttpMethod.Get, $"{bucketName}/{key}", q);

            var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            foreach (var xPart in xResponse.Root!.Elements(Ns + "Part"))
            {
                yield return new PartItem
                {
                    ETag = xPart.Element(Ns + "ETag")?.Value ?? string.Empty,
                    LastModified = DateTimeOffset.Parse(xPart.Element(Ns + "LastModified")?.Value ?? string.Empty, CultureInfo.InvariantCulture),
                    PartNumber = int.Parse(xPart.Element(Ns + "PartNumber")?.Value ?? "0", CultureInfo.InvariantCulture),
                    Size = long.Parse(xPart.Element(Ns + "Size")?.Value ?? "0", CultureInfo.InvariantCulture),
                    ChecksumCRC32 = xPart.Element(Ns + "ChecksumCRC32")?.Value,
                    ChecksumCRC32C = xPart.Element(Ns + "ChecksumCRC32C")?.Value,
                    ChecksumSHA1 = xPart.Element(Ns + "ChecksumSHA1")?.Value,
                    ChecksumSHA256 = xPart.Element(Ns + "ChecksumSHA256")?.Value
                };
            }
            
            partNumberMarker = xResponse.Root!.Element(Ns + "NextPartNumberMarker")?.Value;
            var isTruncated = xResponse.Root!.Element(Ns + "IsTruncated")?.Value == "true";
            if (!isTruncated) break;
        }
    }
    public async IAsyncEnumerable<UploadItem> ListMultipartUploadsAsync(string bucketName, string? delimiter, string? encodingType, string? keyMarker, int pageSize, string? prefix, string? uploadIdMarker, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);

        while (true)
        {
            var q = new QueryParams();
            q.Add("uploads", string.Empty);
            q.AddIfNotNullOrEmpty("delimiter", delimiter);
            q.AddIfNotNullOrEmpty("encoding-type", encodingType);
            q.AddIfNotNullOrEmpty("key-marker", keyMarker);
            if (pageSize > 0)
                q.Add("max-uploads", pageSize.ToString(CultureInfo.InvariantCulture));
            q.AddIfNotNullOrEmpty("prefix", prefix);
            q.AddIfNotNullOrEmpty("upload-id-marker", uploadIdMarker);
            using var req = CreateRequest(HttpMethod.Get, bucketName, q);

            var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            foreach (var xUpload in xResponse.Root!.Elements(Ns + "Upload"))
            {
                yield return new UploadItem
                {
                    UploadId = xUpload.Element(Ns + "UploadId")?.Value ?? string.Empty,
                    Key = xUpload.Element(Ns + "Key")?.Value ?? string.Empty,
                    Initiated = DateTimeOffset.Parse(xUpload.Element(Ns + "Initiated")?.Value ?? string.Empty, CultureInfo.InvariantCulture),
                    StorageClass = xUpload.Element(Ns + "StorageClass")?.Value ?? string.Empty,
                };
            }
            
            keyMarker = xResponse.Root!.Element(Ns + "NextKeyMarker")?.Value;
            uploadIdMarker = xResponse.Root!.Element(Ns + "NextUploadIdMarker")?.Value;
            var isTruncated = xResponse.Root!.Element(Ns + "IsTruncated")?.Value == "true";
            if (!isTruncated) break;
        }
    }

    public async Task<BucketNotification> GetBucketNotificationsAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        
        var query = new QueryParams();
        query.Add("notification", string.Empty);
        
        using var req = CreateRequest(HttpMethod.Get, bucketName, query);
        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        return BucketNotification.Deserialize(xResponse.Root!);
    }

    public async Task SetBucketNotificationsAsync(string bucketName, BucketNotification bucketNotification, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        
        var query = new QueryParams();
        query.Add("notification", string.Empty);

        var xml = bucketNotification.Serialize();
        
        using var req = CreateRequest(HttpMethod.Put, bucketName, xml, query);
        await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<NotificationEvent> ListenBucketNotificationsAsync(string bucketName, IEnumerable<EventType> events, string prefix, string suffix, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        if (events == null) throw new System.ArgumentNullException(nameof(events));

        var eventText = string.Join(",", events);
        if (string.IsNullOrEmpty(eventText))
            throw new System.ArgumentException("No events specified", nameof(events));

        var query = new QueryParams();
        query.Add("ping", "10");
        query.Add("events", eventText);
        query.AddIfNotNullOrEmpty("prefix", prefix);
        query.AddIfNotNullOrEmpty("suffix", suffix);

        using var req = CreateRequest(HttpMethod.Get, bucketName, query);
        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (responseBody.ConfigureAwait(false))
        {
            using var sr = new StreamReader(responseBody);
            while (!sr.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await sr.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(line))
                    break;

                var bucketNotificationEvent = JsonSerializer.Deserialize<BucketNotificationEvent>(line);
                if (bucketNotificationEvent != null)
                {
                    foreach (var e in bucketNotificationEvent.Records)
                        yield return e;
                }
            }
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage req, CancellationToken cancellationToken)
    {
        if (req.Content != null)
        {
            var stream = await req.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            req.Headers.Add("X-Amz-Content-Sha256", hash.ToHexStringLowercase());
            stream.Position = 0;
        }
        else
        {
            req.Headers.Add("X-Amz-Content-Sha256", EmptySha256);
        }

        req.Headers.Add("X-Amz-Date", _timeProvider.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));
        await _requestAuthenticator.AuthenticateAsync(req, _options.Value.Region, "s3", cancellationToken).ConfigureAwait(false);

        using var httpClient = _httpClientFactory.CreateClient(_options.Value.MinioHttpClient);
        var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var xmlData = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(xmlData))
            {
                var xRoot = XDocument.Parse(xmlData).Root;
                if (xRoot != null)
                {
                    var err = new ErrorResponse
                    {
                        Code = xRoot.Element("Code")?.Value ?? string.Empty,
                        Message = xRoot.Element("Message")?.Value ?? string.Empty,
                        BucketName = xRoot.Element("BucketName")?.Value ?? string.Empty,
                        Key = xRoot.Element("Key")?.Value ?? string.Empty,
                        Resource = xRoot.Element("Resource")?.Value ?? string.Empty,
                        RequestId = xRoot.Element("RequestId")?.Value ?? string.Empty,
                        HostId = xRoot.Element("HostId")?.Value ?? string.Empty,
                        Region = xRoot.Element("Region")?.Value ?? string.Empty,
                        Server = xRoot.Element("Server")?.Value ?? string.Empty,
                    };
                    throw new MinioHttpException(req, resp, err);
                }
            }
            throw new MinioHttpException(req, resp, null);
        }

        return resp;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, QueryParams? queryParameters = null)
    {
        var uriBuilder = new StringBuilder();
        uriBuilder.Append(_options.Value.EndPoint);
        if (uriBuilder[^1] != '/')
            uriBuilder.Append('/');
        if (!string.IsNullOrEmpty(path))
            uriBuilder.Append(path);
        if (queryParameters != null)
            uriBuilder.Append(queryParameters);

        return new HttpRequestMessage(method, new Uri(uriBuilder.ToString()));
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, XElement xml, QueryParams? queryParameters = null)
    {
        var req = CreateRequest(method, path, queryParameters);
        req.Content = new XmlHttpContent(new XDocument(xml));
        return req;
    }

    private bool IsGoogleEndpoint => _options.Value.EndPoint.Host == "storage.googleapis.com";
    
    //public readonly record struct PartInfo(int TotalPartsCount, int PartSize, int LastPartSize);

    private static ObjectInfo ToObjectInfo(string key, HttpResponseMessage resp)
    {
        var etag = resp.Headers.ETag!;
        var contentLength = resp.Content.Headers.ContentLength!;
        var lastModified = resp.Content.Headers.LastModified;
        var contentType = resp.Content.Headers.ContentType ?? new MediaTypeHeaderValue("application/octet-stream");
        var expires = resp.Content.Headers.Expires;
        var versionId = resp.Headers.TryGetValue("X-Amz-Version-Id");
        var replicationStatus = resp.Headers.TryGetValue("X-Amz-Replication-Status");
        
        // Headers are case-insensitive, so the metadata
        var metadata =
            resp.Headers
                .Where(kv => PreserveKeys.Any(k => kv.Key.StartsWith(k, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(
                    kv => kv.Key, 
                    kv => string.Join(",", kv.Value),
                    StringComparer.OrdinalIgnoreCase);

        const string metaPrefix = "X-Amz-Meta-"; 
        var userMetadata = 
            metadata
                .Where(kv => kv.Key.StartsWith(metaPrefix, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(
                    kv => kv.Key[metaPrefix.Length..], 
                    kv => string.Join(",", kv.Value),
                    StringComparer.OrdinalIgnoreCase);
        var userTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in resp.Headers.TryGetValues("X-Amz-Tagging"))
        {
            var qs = HttpUtility.ParseQueryString(value);
            foreach (var k in qs.AllKeys)
            {
                if (k != null)
                    userTags[k] = qs[k] ?? string.Empty;
            }
        }
        var tagCount = int.Parse(resp.Headers.TryGetValue("X-Amz-Tagging-Count") ?? "0", CultureInfo.InvariantCulture);
        var restoreValue = resp.Headers.TryGetValue("X-Amz-Restore");
        Restore? restore = null;
        if (!string.IsNullOrEmpty(restoreValue))
        {
            var matches = RestoreRegex.Matches(restoreValue);
            if (matches.Count == 4)
            {
                var ongoingRestore = bool.Parse(matches[1].Value);
                var restoreExpiryDate = !string.IsNullOrEmpty(matches[3].Value) ? (DateTimeOffset?)DateTimeOffset.ParseExact(matches[3].Value, "R", CultureInfo.InvariantCulture) : null;
                restore = new Restore(ongoingRestore, restoreExpiryDate);
            }
        }

        var expirationValue = resp.Headers.TryGetValue("X-Amz-Expiration");
        DateTimeOffset? expirationDate = null;
        string? expirationRuleId = null;
        if (!string.IsNullOrEmpty(expirationValue))
        {
            var matches = ExpirationRegex.Matches(expirationValue);
            if (matches.Count == 3)
            {
                expirationDate = DateTimeOffset.ParseExact(matches[1].Value, "R", CultureInfo.InvariantCulture);
                expirationRuleId = matches[2].Value;
            }
            
        }
        
        var deleteMarker = resp.Headers.TryGetValue("X-Amz-Delete-Marker") == "true";
        
        return new ObjectInfo
        {
            Etag = etag,
            Key = key,
            ContentLength = contentLength,
            LastModified = lastModified,
            ContentType = contentType,
            Expires = expires,
            VersionId = versionId,
            IsDeleteMarker = deleteMarker,
            ReplicationStatus = replicationStatus,
            Expiration = expirationDate,
            ExpirationRuleId = expirationRuleId,
            
            Metadata = metadata,
            UserMetadata = userMetadata,
            UserTags = userTags,
            UserTagCount = tagCount,
            Restore = restore,

            // Checksum values
            ChecksumCRC32 = resp.Headers.TryGetValue("x-amz-checksum-crc32"),
            ChecksumCRC32C = resp.Headers.TryGetValue("x-amz-checksum-crc32c"),
            ChecksumSHA1 = resp.Headers.TryGetValue("x-amz-checksum-sha1"),
            ChecksumSHA256 = resp.Headers.TryGetValue("x-amz-checksum-sha256"),
        };
    }
}
