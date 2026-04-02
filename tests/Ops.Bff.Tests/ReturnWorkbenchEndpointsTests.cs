using System.Net;
using System.Net.Http.Json;
using Ops.Bff.Tests.TestHost;
using Ops.Bff.Tests.TestDoubles;
using Shared.Contracts.Approvals;
using Shared.Contracts.Common;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Ops.Bff.Tests;

public sealed class ReturnWorkbenchEndpointsTests
{
    [Fact]
    public async Task Get_return_workbench_should_return_suggestion_and_approval_summary()
    {
        var returnOrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await using var app = new BffTestApplicationFactory(
            new StubDomainServiceClient
            {
                PendingApprovals = 3,
                ReturnOrder = new ReturnOrderDto(
                    returnOrderId,
                    "RMA-001",
                    "Broken",
                    "PendingInspection",
                    "Damaged shell")
            },
            new StubAgentRuntimeClient
            {
                FailureCount = 1,
                GetDispositionSuggestionAsyncHandler = static (returnOrderId, _) => Task.FromResult<DispositionSuggestionDto?>(new DispositionSuggestionDto(
                    returnOrderId,
                    "Scrap",
                    "High",
                    [new CitationDto("sop", "doc-1", "v1", "Broken items should be scrapped.")],
                    "Pending"))
            });

        var client = app.CreateClient();

        var response = await client.GetAsync($"/api/returns/workbench/{returnOrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ReturnWorkbenchViewDto>();
        Assert.NotNull(payload);
        Assert.Equal(returnOrderId, payload!.Order.ReturnOrderId);
        Assert.Equal("Pending", payload.Suggestion.ApprovalStatus);
    }

    [Fact]
    public async Task Get_return_workbench_should_degrade_when_runtime_suggestion_is_unavailable()
    {
        var returnOrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await using var app = new BffTestApplicationFactory(
            new StubDomainServiceClient
            {
                ReturnOrder = new ReturnOrderDto(
                    returnOrderId,
                    "RMA-001",
                    "Broken",
                    "PendingInspection",
                    "Damaged shell")
            },
            new StubAgentRuntimeClient());

        var client = app.CreateClient();

        var response = await client.GetAsync($"/api/returns/workbench/{returnOrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ReturnWorkbenchViewDto>();
        Assert.NotNull(payload);
        Assert.Equal("Unavailable", payload!.Suggestion.ApprovalStatus);
        Assert.Equal("PendingInspection", payload.Order.Status);
        Assert.Empty(payload.Suggestion.Citations);
    }
}
