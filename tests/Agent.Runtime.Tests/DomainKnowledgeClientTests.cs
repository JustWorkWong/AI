using System.Net;
using Agent.Runtime.Clients;

namespace Agent.Runtime.Tests;

public sealed class DomainKnowledgeClientTests
{
    [Fact]
    public async Task Get_return_order_should_return_null_when_domain_service_returns_404()
    {
        var client = new DomainKnowledgeClient(new HttpClient(new StubHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("http://runtime-test")
        });

        var result = await client.GetReturnOrderAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class StubHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
