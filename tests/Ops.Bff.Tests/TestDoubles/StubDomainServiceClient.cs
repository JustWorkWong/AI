using Ops.Bff.Clients;
using Shared.Contracts.Returns;

namespace Ops.Bff.Tests.TestDoubles;

internal sealed class StubDomainServiceClient : IDomainServiceClient
{
    public int PendingApprovals { get; init; }

    public ReturnOrderDto? ReturnOrder { get; init; }

    public Task<int> GetPendingApprovalsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(PendingApprovals);

    public Task<ReturnOrderDto?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
        Task.FromResult(ReturnOrder);
}
