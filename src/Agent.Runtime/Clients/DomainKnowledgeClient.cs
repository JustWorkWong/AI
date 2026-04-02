using System.Net.Http.Json;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Agent.Runtime.Clients;

public interface IDomainKnowledgeClient
{
    Task<ReturnOrderDto?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<HistoricalCaseDto>> GetHistoricalCasesAsync(Guid returnOrderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SopCandidateDto>> SearchSopCandidatesAsync(
        string operationCode,
        string stepCode,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SopChunkDto>> RetrieveSopChunksAsync(
        RetrieveSopChunksQuery query,
        CancellationToken cancellationToken);
}

public sealed class DomainKnowledgeClient(HttpClient httpClient) : IDomainKnowledgeClient
{
    public async Task<ReturnOrderDto?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"/internal/returns/{returnOrderId}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReturnOrderDto>(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<HistoricalCaseDto>> GetHistoricalCasesAsync(
        Guid returnOrderId,
        CancellationToken cancellationToken)
    {
        var payload = await httpClient.GetFromJsonAsync<IReadOnlyList<HistoricalCaseDto>>(
            $"/internal/returns/{returnOrderId}/historical-cases",
            cancellationToken);

        return payload ?? [];
    }

    public async Task<IReadOnlyList<SopCandidateDto>> SearchSopCandidatesAsync(
        string operationCode,
        string stepCode,
        CancellationToken cancellationToken)
    {
        var payload = await httpClient.GetFromJsonAsync<IReadOnlyList<SopCandidateDto>>(
            $"/internal/sop/candidates?operationCode={Uri.EscapeDataString(operationCode)}&stepCode={Uri.EscapeDataString(stepCode)}",
            cancellationToken);

        return payload ?? [];
    }

    public async Task<IReadOnlyList<SopChunkDto>> RetrieveSopChunksAsync(
        RetrieveSopChunksQuery query,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/internal/sop/chunks/search",
            query,
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<SopChunkDto>>(cancellationToken: cancellationToken);
        return payload ?? [];
    }
}
