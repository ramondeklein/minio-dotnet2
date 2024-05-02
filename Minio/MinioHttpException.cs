using System.Globalization;
using System.Text;
using Minio.Model;

namespace Minio;

#pragma warning disable CA1032  // Don't need the default exception constructors

public class MinioHttpException : MinioException
{
    public HttpRequestMessage Request { get; }
    public HttpResponseMessage Response { get; }
    public ErrorResponse? Error { get; }

    internal MinioHttpException(HttpRequestMessage request, HttpResponseMessage response, ErrorResponse? error)
        : base(GetMessage(request, response, error))
    {
        Request = request;
        Response = response;
        Error = error;
    }

    private static string GetMessage(HttpRequestMessage request, HttpResponseMessage response, ErrorResponse? error)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"{request.Method} {request.RequestUri} returned HTTP status-code {(int)response.StatusCode} ({response.StatusCode})");
        if (!string.IsNullOrEmpty(error?.Message))
            sb.Append(CultureInfo.InvariantCulture, $": {error.Message}");
        return sb.ToString();
    }
}