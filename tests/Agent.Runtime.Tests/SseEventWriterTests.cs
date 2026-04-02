using System.Text;
using Agent.Runtime.Streaming;
using Microsoft.AspNetCore.Http;

namespace Agent.Runtime.Tests;

public sealed class SseEventWriterTests
{
    [Fact]
    public async Task Write_should_serialize_event_payload_with_camel_case_contract()
    {
        var sessionId = Guid.NewGuid();
        var response = new DefaultHttpContext().Response;
        await using var stream = new MemoryStream();
        response.Body = stream;

        var writer = new SseEventWriter();
        await writer.WriteAsync(response, AgUiEventMapper.MapHeartbeat(sessionId), CancellationToken.None);

        stream.Position = 0;
        var payload = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Contains("event: heartbeat", payload);
        Assert.Contains("\"type\":\"heartbeat\"", payload);
        Assert.Contains("\"traceId\":\"trace-demo\"", payload);
        Assert.Contains($"\"sessionId\":\"{sessionId}\"", payload);
    }
}
