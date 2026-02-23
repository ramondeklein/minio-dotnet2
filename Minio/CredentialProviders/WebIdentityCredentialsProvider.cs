using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Minio.Helpers;
using Minio.Model;

namespace Minio.CredentialProviders;

public class WebIdentityProvider : ICredentialsProvider
{
    private readonly XNamespace Ns = "https://sts.amazonaws.com/doc/2011-06-15/";
    
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly IOptions<WebIdentityCredentialsOptions> _options;

    public WebIdentityProvider(IHttpClientFactory httpClientFactory, IAccessTokenProvider accessTokenProvider, IOptions<WebIdentityCredentialsOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _accessTokenProvider = accessTokenProvider;
        _options = options;
    }
    
    public async ValueTask<Credentials> GetCredentialsAsync(CancellationToken cancellationToken)
    {
        var accessToken = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(accessToken)) throw new InvalidOperationException("No access token");

        var opts = _options.Value;
        var query = new QueryParams();
        query.Add("Action", "AssumeRoleWithWebIdentity");
        query.Add("Version", "2011-06-15");
        query.Add("WebIdentityToken", accessToken);
        query.Add("DurationSeconds", opts.DurationSeconds.ToString(CultureInfo.InvariantCulture));
        query.AddIfNotNullOrEmpty("Policy", opts.Policy);
        query.AddIfNotNullOrEmpty("RoleARN", opts.RoleARN);
        query.AddIfNotNullOrEmpty("TokenRevokeType", opts.TokenRevokeType);
        
        var builder = new UriBuilder(opts.StsEndPoint)
        {
            Query = query.ToString()
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, builder.Uri);
        using var httpClient = _httpClientFactory.CreateClient(opts.MinioHttpClient);
        var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var responseData = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var contentType = resp.Content.Headers.ContentType?.MediaType; 
            if (contentType == "application/xml" && !string.IsNullOrEmpty(responseData))
            {
                var xRoot = XDocument.Parse(responseData).Root;
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
        
        var responseBody = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var xResponse = await XDocument.LoadAsync(responseBody, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        
        var xCreds = xResponse.Root?.Element(Ns + "AssumeRoleWithWebIdentityResult")?.Element(Ns + "Credentials");
        var exp = xCreds?.Element(Ns + "Expiration")?.Value;
        return new Credentials
        {
            AccessKey = xCreds?.Element(Ns + "AccessKeyId")?.Value ?? string.Empty,
            SecretKey = xCreds?.Element(Ns + "SecretAccessKey")?.Value ?? string.Empty,
            SessionToken = xCreds?.Element(Ns + "SessionToken")?.Value ?? string.Empty,
            Expiration = exp != null ? DateTime.Parse(exp, null, DateTimeStyles.RoundtripKind) : null,
        };
    }
}