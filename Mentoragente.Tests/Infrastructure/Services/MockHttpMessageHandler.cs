using System.Net;
using System.Text;

namespace Mentoragente.Tests.Infrastructure.Services;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _handler(request);
    }

    public static MockHttpMessageHandler CreateSuccessHandler(string responseContent)
    {
        return new MockHttpMessageHandler(async request =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };
            return await Task.FromResult(response);
        });
    }

    public static MockHttpMessageHandler CreateErrorHandler(HttpStatusCode statusCode, string errorMessage)
    {
        return new MockHttpMessageHandler(async request =>
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(errorMessage, Encoding.UTF8, "application/json")
            };
            return await Task.FromResult(response);
        });
    }
}

