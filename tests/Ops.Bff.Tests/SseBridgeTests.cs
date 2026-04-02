using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Ops.Bff.Tests.TestHost;
using Ops.Bff.Tests.TestDoubles;

namespace Ops.Bff.Tests;

public sealed class SseBridgeTests
{
    [Fact]
    public async Task Get_sop_events_should_bridge_upstream_sse_payload()
    {
        await using var app = new BffTestApplicationFactory(
            new StubDomainServiceClient(),
            new StubAgentRuntimeClient
            {
                ProxySseAsyncHandler = async (sessionId, response, cancellationToken) =>
                {
                    response.StatusCode = StatusCodes.Status200OK;
                    response.ContentType = "text/event-stream";

                    await response.Body.WriteAsync(
                        Encoding.UTF8.GetBytes($"event: heartbeat\ndata: {{\"sessionId\":\"{sessionId}\"}}\n\n"),
                        cancellationToken);
                }
            });

        var client = app.CreateClient();
        var sessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var response = await client.GetAsync($"/api/sop/sessions/{sessionId}/events");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("event: heartbeat", payload);
        Assert.Contains(sessionId.ToString(), payload);
    }
}
