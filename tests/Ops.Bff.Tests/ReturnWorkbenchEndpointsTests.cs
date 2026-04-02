using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ops.Bff.Clients;
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
        var returnOrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var response = await client.GetAsync($"/api/returns/workbench/{returnOrderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ReturnWorkbenchViewDto>();
        Assert.NotNull(payload);
        Assert.Equal(returnOrderId, payload!.Order.ReturnOrderId);
        Assert.Equal("Pending", payload.Suggestion.ApprovalStatus);
    }

    private sealed class StubDomainServiceClient : IDomainServiceClient
    {
        public Task<int> GetPendingApprovalsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(3);

        public Task<ReturnOrderDto?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<ReturnOrderDto?>(new ReturnOrderDto(
                returnOrderId,
                "RMA-001",
                "Broken",
                "PendingInspection",
                "Damaged shell"));
    }

    private sealed class StubAgentRuntimeClient : IAgentRuntimeClient
    {
        public Task<int> GetFailureCountAsync(CancellationToken cancellationToken) =>
            Task.FromResult(1);

        public Task<DispositionSuggestionDto?> GetDispositionSuggestionAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<DispositionSuggestionDto?>(new DispositionSuggestionDto(
                returnOrderId,
                "Scrap",
                "High",
                [new CitationDto("sop", "doc-1", "v1", "Broken items should be scrapped.")],
                "Pending"));

        public Task<DispositionExecutionResultDto?> ExecuteDispositionAsync(
            Guid returnOrderId,
            ExecuteDispositionRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult<DispositionExecutionResultDto?>(null);

        public Task<DispositionExecutionTraceDto?> GetDispositionTraceAsync(
            Guid workflowInstanceId,
            CancellationToken cancellationToken) =>
            Task.FromResult<DispositionExecutionTraceDto?>(null);

        public Task<DispositionExecutionResultDto?> DecideDispositionApprovalAsync(
            Guid workflowInstanceId,
            ApprovalDecisionRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult<DispositionExecutionResultDto?>(null);

        public Task<SopExecutionViewDto?> AdvanceSopSessionAsync(
            Guid sessionId,
            AdvanceSopStepRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult<SopExecutionViewDto?>(null);

        public Task ProxySseAsync(Guid sessionId, HttpResponse response, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
