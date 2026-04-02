using System.Net.Http.Json;
using Shared.Contracts.Returns;

namespace Ops.Bff.Clients;

public interface IDomainServiceClient
{
    Task<int> GetPendingApprovalsAsync(CancellationToken cancellationToken);

    Task<ReturnOrderDto?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken);
}

public sealed class DomainServiceClient(HttpClient httpClient) : IDomainServiceClient
{
    public async Task<int> GetPendingApprovalsAsync(CancellationToken cancellationToken)
    {
        var result = await httpClient.GetFromJsonAsync<int?>(
            "/internal/dashboard/pending-approvals",
            cancellationToken);

        return result ?? 0;
    }

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
}
