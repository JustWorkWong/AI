using System.Net.Http.Json;
using Shared.Contracts.Sop;

namespace Ops.Bff.Clients;

public interface IAgentRuntimeClient
{
    Task<int> GetFailureCountAsync(CancellationToken cancellationToken);

    Task<object?> GetDispositionSuggestionAsync(Guid returnOrderId, CancellationToken cancellationToken);

    Task<object?> AdvanceSopSessionAsync(
        Guid sessionId,
        AdvanceSopStepRequest request,
        CancellationToken cancellationToken);

    Task ProxySseAsync(Guid sessionId, HttpResponse response, CancellationToken cancellationToken);
}

public sealed class AgentRuntimeClient(HttpClient httpClient) : IAgentRuntimeClient
{
    public async Task<int> GetFailureCountAsync(CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<int?>(
            "/internal/runtime/failures/count",
            cancellationToken);

        return result ?? 0;
    }

    public Task<object?> GetDispositionSuggestionAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
        httpClient.GetFromJsonAsync<object>(
            $"/internal/runtime/dispositions/{returnOrderId}",
            cancellationToken);

    public async Task<object?> AdvanceSopSessionAsync(
        Guid sessionId,
        AdvanceSopStepRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/internal/runtime/sop/{sessionId}/steps",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<object>(cancellationToken: cancellationToken);
    }

    public Task ProxySseAsync(Guid sessionId, HttpResponse response, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
