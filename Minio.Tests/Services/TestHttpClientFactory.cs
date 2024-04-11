namespace Minio.Tests.Services;

public sealed class TestHttpClientFactory : IHttpClientFactory, IDisposable
{
    private sealed class MockHttpMessageHandler : DelegatingHandler
    {
        private readonly Action<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Action<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }
        
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken _)
        {
            if (string.IsNullOrEmpty(request.Headers.Host))
                request.Headers.Host = request.RequestUri?.Authority;
            var response = new HttpResponseMessage { RequestMessage = request };
            _handler(request, response);
            return Task.FromResult(response);
        }
    }
    
    private readonly HttpMessageHandler _messageHandler;

    public TestHttpClientFactory(Action<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _messageHandler = new MockHttpMessageHandler(handler);
    }
    
    public HttpClient CreateClient(string name) => new(_messageHandler);

    public void Dispose()
    {
        _messageHandler.Dispose();
    }
}