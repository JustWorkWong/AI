using System.Net.Http.Json;

namespace Ops.Bff.Clients;

public interface IDomainServiceClient
{
    Task<int> GetPendingApprovalsAsync(CancellationToken cancellationToken);

    Task<object?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken);
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

    public Task<object?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
        httpClient.GetFromJsonAsync<object>(
            $"/internal/returns/{returnOrderId}",
            cancellationToken);
}
