using System.Text.Json;
using Shared.Contracts.Realtime;

namespace Agent.Runtime.Streaming;

public sealed class SseEventWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task WriteAsync(HttpResponse response, AgUiEvent evt, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(evt, JsonOptions);
        await response.WriteAsync($"event: {evt.Type}\n", cancellationToken);
        await response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
