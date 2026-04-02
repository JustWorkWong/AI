using System.Net;
using System.Text;
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

public sealed class SseBridgeTests
{
    [Fact]
    public async Task Get_sop_events_should_bridge_upstream_sse_payload()
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
        var sessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var response = await client.GetAsync($"/api/sop/sessions/{sessionId}/events");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("event: heartbeat", payload);
        Assert.Contains(sessionId.ToString(), payload);
    }

    private sealed class StubDomainServiceClient : IDomainServiceClient
    {
        public Task<int> GetPendingApprovalsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<ReturnOrderDto?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<ReturnOrderDto?>(null);
    }

    private sealed class StubAgentRuntimeClient : IAgentRuntimeClient
    {
        public Task<int> GetFailureCountAsync(CancellationToken cancellationToken) =>
            Task.FromResult(0);

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

        public async Task ProxySseAsync(Guid sessionId, HttpResponse response, CancellationToken cancellationToken)
        {
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "text/event-stream";

            await response.Body.WriteAsync(
                Encoding.UTF8.GetBytes($"event: heartbeat\ndata: {{\"sessionId\":\"{sessionId}\"}}\n\n"),
                cancellationToken);
        }
    }
}
