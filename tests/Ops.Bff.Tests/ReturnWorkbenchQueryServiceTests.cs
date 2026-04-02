using Ops.Bff.Clients;
using Ops.Bff.Queries;
using Ops.Bff.Tests.TestDoubles;
using Shared.Contracts.Returns;
using System.Net.Http;

namespace Ops.Bff.Tests;

public sealed class ReturnWorkbenchQueryServiceTests
{
    [Fact]
    public async Task Get_view_should_return_unavailable_suggestion_when_runtime_throws()
    {
        var service = new ReturnWorkbenchQueryService(
            new StubDomainServiceClient
            {
                PendingApprovals = 3,
                ReturnOrder = new ReturnOrderDto(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "RMA-001",
                    "Broken",
                    "PendingInspection",
                    "Damaged shell")
            },
            new StubAgentRuntimeClient
            {
                GetDispositionSuggestionAsyncHandler = static (_, _) => throw new HttpRequestException("runtime unavailable")
            });

        var result = await service.GetViewAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Unavailable", result!.Suggestion.ApprovalStatus);
        Assert.Equal("PendingInspection", result.Order.Status);
    }
}
