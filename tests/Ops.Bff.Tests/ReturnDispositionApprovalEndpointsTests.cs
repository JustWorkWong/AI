using System.Net;
using System.Net.Http.Json;
using Ops.Bff.Tests.TestHost;
using Ops.Bff.Tests.TestDoubles;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;

namespace Ops.Bff.Tests;

public sealed class ReturnDispositionApprovalEndpointsTests
{
    [Fact]
    public async Task Post_approval_should_return_completed_runtime_result()
    {
        await using var app = new BffTestApplicationFactory(
            new StubDomainServiceClient(),
            new StubAgentRuntimeClient
            {
                DecideDispositionApprovalAsyncHandler = static (workflowInstanceId, _, _) => Task.FromResult<DispositionExecutionResultDto?>(new DispositionExecutionResultDto(
                    workflowInstanceId,
                    "Completed",
                    Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    "Scrap"))
            });

        var client = app.CreateClient();
        var workflowInstanceId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/returns/workbench/executions/{workflowInstanceId}/approval",
            new ApprovalDecisionRequest("Approve", "manager-1"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DispositionExecutionResultDto>();
        Assert.NotNull(payload);
        Assert.Equal("Completed", payload!.Status);
    }
}
