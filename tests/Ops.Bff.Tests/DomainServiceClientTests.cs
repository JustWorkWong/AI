using System.Net;
using Ops.Bff.Clients;
using Shared.Contracts.Returns;

namespace Ops.Bff.Tests;

public sealed class DomainServiceClientTests
{
    [Fact]
    public async Task Get_return_order_should_return_null_when_domain_service_returns_404()
    {
        var client = new DomainServiceClient(new HttpClient(new StubHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("http://bff-test")
        });

        var result = await client.GetReturnOrderAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Get_disposition_suggestion_should_return_null_when_runtime_returns_404()
    {
        var client = new AgentRuntimeClient(new HttpClient(new StubHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("http://bff-test")
        });

        var result = await client.GetDispositionSuggestionAsync(Guid.NewGuid(), CancellationToken.None);

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
