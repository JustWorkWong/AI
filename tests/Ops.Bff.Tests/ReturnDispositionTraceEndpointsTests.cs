using System.Net;
using System.Net.Http.Json;
using Ops.Bff.Tests.TestHost;
using Ops.Bff.Tests.TestDoubles;
using Shared.Contracts.Returns;

namespace Ops.Bff.Tests;

public sealed class ReturnDispositionTraceEndpointsTests
{
    [Fact]
    public async Task Get_trace_should_return_runtime_execution_trace()
    {
        await using var app = new BffTestApplicationFactory(
            new StubDomainServiceClient(),
            new StubAgentRuntimeClient
            {
                GetDispositionTraceAsyncHandler = static (workflowInstanceId, _) => Task.FromResult<DispositionExecutionTraceDto?>(new DispositionExecutionTraceDto(
                    workflowInstanceId,
                    "return-disposition-execute",
                    "WaitingApproval",
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    [
                        new ToolInvocationDto(Guid.NewGuid(), "GetReturnOrderTool", "Completed", "trace-a", 12, "{}", "order", null)
                    ],
                    [
                        new WorkflowCheckpointDto(Guid.NewGuid(), 1, "approval", "{\"approvalReferenceId\":\"44444444-4444-4444-4444-444444444444\"}")
                    ]))
            });

        var client = app.CreateClient();
        var workflowInstanceId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var response = await client.GetAsync($"/api/returns/workbench/executions/{workflowInstanceId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DispositionExecutionTraceDto>();
        Assert.NotNull(payload);
        Assert.Equal(workflowInstanceId, payload!.WorkflowInstanceId);
        Assert.Single(payload.ToolInvocations);
        Assert.Single(payload.Checkpoints);
    }
}
