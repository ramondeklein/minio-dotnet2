using Minio.Tests.Helpers;
using Minio.Tests.Services;
using Xunit;

namespace Minio.Tests.UnitTests;

public class HttpAssertsTests
{
    [Fact]
    public async Task CheckHeaders()
    {
        var httpClientFactory = new TestHttpClientFactory((req, resp) =>
        {
            req.AssertHeaders("host: example.com");
            resp.Headers.SetRawHeader("Content-Type", "text/plain");
        });
        using var httpClient = httpClientFactory.CreateClient("");
        var resp = await httpClient.GetAsync("https://example.com");
        resp.AssertHeaders("content-type: text/plain");
    }
}
