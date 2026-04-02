using Ops.Bff.Clients;
using Ops.Bff.Presenters;
using Shared.Contracts.Returns;

namespace Ops.Bff.Queries;

public interface IReturnWorkbenchQueryService
{
    Task<ReturnWorkbenchViewDto?> GetViewAsync(Guid returnOrderId, CancellationToken cancellationToken);
}

public sealed class ReturnWorkbenchQueryService(
    IDomainServiceClient domainClient,
    IAgentRuntimeClient runtimeClient) : IReturnWorkbenchQueryService
{
    public async Task<ReturnWorkbenchViewDto?> GetViewAsync(
        Guid returnOrderId,
        CancellationToken cancellationToken)
    {
        var order = await domainClient.GetReturnOrderAsync(returnOrderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var suggestion = ReturnWorkbenchPresenter.CoalesceSuggestion(
            await TryGetSuggestionAsync(returnOrderId, cancellationToken),
            returnOrderId);

        return new ReturnWorkbenchViewDto(order, suggestion);
    }

    private async Task<DispositionSuggestionDto?> TryGetSuggestionAsync(
        Guid returnOrderId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await runtimeClient.GetDispositionSuggestionAsync(returnOrderId, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
