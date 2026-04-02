using System.Net.Http.Json;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Ops.Bff.Clients;

public interface IAgentRuntimeClient
{
    Task<int> GetFailureCountAsync(CancellationToken cancellationToken);

    Task<DispositionSuggestionDto?> GetDispositionSuggestionAsync(Guid returnOrderId, CancellationToken cancellationToken);

    Task<DispositionExecutionResultDto?> ExecuteDispositionAsync(
        Guid returnOrderId,
        ExecuteDispositionRequest request,
        CancellationToken cancellationToken);

    Task<DispositionExecutionTraceDto?> GetDispositionTraceAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken);

    Task<DispositionExecutionResultDto?> DecideDispositionApprovalAsync(
        Guid workflowInstanceId,
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken);

    Task<SopExecutionViewDto?> AdvanceSopSessionAsync(
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

    public Task<DispositionSuggestionDto?> GetDispositionSuggestionAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
        GetDispositionSuggestionCoreAsync(returnOrderId, cancellationToken);

    public async Task<DispositionExecutionResultDto?> ExecuteDispositionAsync(
        Guid returnOrderId,
        ExecuteDispositionRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/internal/runtime/dispositions/{returnOrderId}/execute",
            request,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DispositionExecutionResultDto>(cancellationToken: cancellationToken);
    }

    public async Task<DispositionExecutionTraceDto?> GetDispositionTraceAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"/internal/runtime/dispositions/executions/{workflowInstanceId}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DispositionExecutionTraceDto>(cancellationToken: cancellationToken);
    }

    public async Task<DispositionExecutionResultDto?> DecideDispositionApprovalAsync(
        Guid workflowInstanceId,
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/internal/runtime/dispositions/executions/{workflowInstanceId}/approval",
            request,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            throw new ArgumentException("Runtime rejected the approval decision request.", nameof(request));
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DispositionExecutionResultDto>(cancellationToken: cancellationToken);
    }

    public async Task<SopExecutionViewDto?> AdvanceSopSessionAsync(
        Guid sessionId,
        AdvanceSopStepRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/internal/runtime/sop/{sessionId}/steps",
            request,
            cancellationToken);

        return await response.Content.ReadFromJsonAsync<SopExecutionViewDto>(cancellationToken: cancellationToken);
    }

    public async Task ProxySseAsync(Guid sessionId, HttpResponse response, CancellationToken cancellationToken)
    {
        using var upstream = await httpClient.GetAsync(
            $"/internal/runtime/sop/{sessionId}/events",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        upstream.EnsureSuccessStatusCode();
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = upstream.Content.Headers.ContentType?.MediaType ?? "text/event-stream";
        await upstream.Content.CopyToAsync(response.Body, cancellationToken);
    }

    private async Task<DispositionSuggestionDto?> GetDispositionSuggestionCoreAsync(
        Guid returnOrderId,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"/internal/runtime/dispositions/{returnOrderId}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DispositionSuggestionDto>(cancellationToken: cancellationToken);
    }
}
