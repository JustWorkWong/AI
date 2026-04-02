using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ops.Bff.Clients;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Ops.Bff.Tests;

public sealed class ReturnDispositionTraceEndpointsTests
{
    [Fact]
    public async Task Get_trace_should_return_runtime_execution_trace()
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IDomainServiceClient>();
                    services.RemoveAll<IAgentRuntimeClient>();
                    services.AddSingleton<IDomainServiceClient>(new StubDomainServiceClient());
                    services.AddSingleton<IAgentRuntimeClient>(new StubAgentRuntimeClient());
                });
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

    private sealed class StubDomainServiceClient : IDomainServiceClient
    {
        public Task<int> GetPendingApprovalsAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<ReturnOrderDto?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<ReturnOrderDto?>(null);
    }

    private sealed class StubAgentRuntimeClient : IAgentRuntimeClient
    {
        public Task<int> GetFailureCountAsync(CancellationToken cancellationToken) => Task.FromResult(0);

        public Task<DispositionSuggestionDto?> GetDispositionSuggestionAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<DispositionSuggestionDto?>(null);

        public Task<DispositionExecutionResultDto?> ExecuteDispositionAsync(
            Guid returnOrderId,
            ExecuteDispositionRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult<DispositionExecutionResultDto?>(null);

        public Task<DispositionExecutionTraceDto?> GetDispositionTraceAsync(
            Guid workflowInstanceId,
            CancellationToken cancellationToken) =>
            Task.FromResult<DispositionExecutionTraceDto?>(new DispositionExecutionTraceDto(
                workflowInstanceId,
                "return-disposition-execute",
                "WaitingApproval",
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                [
                    new ToolInvocationDto(Guid.NewGuid(), "GetReturnOrderTool", "Completed", "trace-a", 12, "{}", "order", null)
                ],
                [
                    new WorkflowCheckpointDto(Guid.NewGuid(), 1, "approval", "{\"approvalReferenceId\":\"44444444-4444-4444-4444-444444444444\"}")
                ]));

        public Task<SopExecutionViewDto?> AdvanceSopSessionAsync(Guid sessionId, AdvanceSopStepRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<SopExecutionViewDto?>(null);

        public Task ProxySseAsync(Guid sessionId, HttpResponse response, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
