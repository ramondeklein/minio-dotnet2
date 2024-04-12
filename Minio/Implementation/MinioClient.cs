using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio.Helpers;

#if NET6_0
using ArgumentException = Shims.ArgumentException; 
using ArgumentNullException = Shims.ArgumentNullException; 
using SHA256 = Shims.SHA256; 
#endif

namespace Minio.Implementation;

internal class MinioClient : IMinioClient
{
    private static readonly XNamespace Ns = "http://s3.amazonaws.com/doc/2006-03-01/";
    private const string EmptySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"; 

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

    public async Task<string> MakeBucketAsync(string bucketName, string? location, bool objectLock, CancellationToken cancellationToken)
    {
        var xml = new XElement(Ns + "CreateBucketConfiguration");
        if (!string.IsNullOrEmpty(location) && location != "us-east-1")
        {
            xml.Add(new XElement(Ns + "Location",
                new XElement(Ns + "Name", location)));
        }

        using var req = CreateRequest(HttpMethod.Put, bucketName, xml);
        if (objectLock)
            req.Headers.Add("X-Amz-Bucket-Object-Lock-Enabled", "true");

        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        return resp.GetHeaderValue("Location") ?? "";
    }

    public async Task PutObjectAsync(string bucketName, string key, Stream stream, string? contentType, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(stream);

        using var req = CreateRequest(HttpMethod.Put, $"{bucketName}/{key}");
        req.Content = new StreamContent(stream);
        if (!string.IsNullOrEmpty(contentType))
            req.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream> GetObjectAsync(string bucketName, string key, string? versionId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var q = new QueryParams();
        q.AddIfNotNullOrEmpty("versionId", versionId);
        using var req = CreateRequest(HttpMethod.Get, $"{bucketName}/{key}", q);

        var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

        return await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<ObjectItem> ListObjectsAsync(string bucketName, string? prefix, string? delimiter, string? encodingType, string? startAfter, int maxKeys, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(bucketName);

        string? continuationToken = null;
        do
        {
            var q = new QueryParams();
            q.Add("list-type", "2");
            q.AddIfNotNullOrEmpty("continuation-token", continuationToken);
            q.AddIfNotNullOrEmpty("prefix", prefix);
            q.AddIfNotNullOrEmpty("delimiter", delimiter);
            q.AddIfNotNullOrEmpty("encoding-type", encodingType);
            q.AddIfNotNullOrEmpty("start-after", startAfter);
            if (maxKeys > 0)
                q.Add("max-keys", maxKeys.ToString(CultureInfo.InvariantCulture));
            using var req = CreateRequest(HttpMethod.Get, bucketName, q);

            var resp = await SendRequestAsync(req, cancellationToken).ConfigureAwait(false);

            var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);

            foreach (var xContent in xResponse.Root!.Elements(Ns + "Contents"))
            {
                var objItem = new ObjectItem
                {
                    Key = xContent.Element(Ns + "Key")?.Value ?? string.Empty,
                    ETag = xContent.Element(Ns + "ETag")?.Value ?? string.Empty,
                    Size = long.TryParse(xContent.Element(Ns + "Size")?.Value, out var size) ? size : -1,
                    StorageClass = xContent.Element(Ns + "Key")?.Value ?? string.Empty
                };
                yield return objItem;
            }
            
            continuationToken = xResponse.Root!.Element(Ns + "NextContinuationToken")?.Value;
        } while (continuationToken != null);
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
        var resp = await httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            throw new MinioHttpException(req, resp);

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
}