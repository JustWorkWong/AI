using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ops.Bff.Clients;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Ops.Bff.Tests;

public sealed class ReturnDispositionApprovalEndpointsTests
{
    [Fact]
    public async Task Post_approval_should_return_completed_runtime_result()
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
        var workflowInstanceId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/returns/workbench/executions/{workflowInstanceId}/approval",
            new ApprovalDecisionRequest("Approve", "manager-1"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DispositionExecutionResultDto>();
        Assert.NotNull(payload);
        Assert.Equal("Completed", payload!.Status);
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

        public Task<DispositionExecutionResultDto?> ExecuteDispositionAsync(Guid returnOrderId, ExecuteDispositionRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<DispositionExecutionResultDto?>(null);

        public Task<DispositionExecutionTraceDto?> GetDispositionTraceAsync(Guid workflowInstanceId, CancellationToken cancellationToken) =>
            Task.FromResult<DispositionExecutionTraceDto?>(null);

        public Task<DispositionExecutionResultDto?> DecideDispositionApprovalAsync(
            Guid workflowInstanceId,
            ApprovalDecisionRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult<DispositionExecutionResultDto?>(new DispositionExecutionResultDto(
                workflowInstanceId,
                "Completed",
                Guid.Parse("77777777-7777-7777-7777-777777777777"),
                "Scrap"));

        public Task<SopExecutionViewDto?> AdvanceSopSessionAsync(Guid sessionId, AdvanceSopStepRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<SopExecutionViewDto?>(null);

        public Task ProxySseAsync(Guid sessionId, HttpResponse response, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
