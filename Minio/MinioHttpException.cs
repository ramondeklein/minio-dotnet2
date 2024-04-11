namespace Minio;

public class MinioHttpException : Exception
{
    public HttpRequestMessage Request { get; }
    public HttpResponseMessage Response { get; }

    internal MinioHttpException(HttpRequestMessage request, HttpResponseMessage response)
        : base(GetMessage(request, response))
    {
        Request = request;
        Response = response;
    }

    private static string GetMessage(HttpRequestMessage request, HttpResponseMessage response)
    {
        return $"{request.Method} {request.RequestUri} returned HTTP status-code {(int)response.StatusCode} ({response.StatusCode})";
    }
}