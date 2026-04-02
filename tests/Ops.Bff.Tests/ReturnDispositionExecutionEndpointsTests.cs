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

public sealed class ReturnDispositionExecutionEndpointsTests
{
    [Fact]
    public async Task Post_execute_should_return_runtime_execution_result()
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
        var returnOrderId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/returns/workbench/{returnOrderId}/execute",
            new ExecuteDispositionRequest("idem-bff"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<DispositionExecutionResultDto>();
        Assert.NotNull(payload);
        Assert.Equal("Completed", payload!.Status);
        Assert.Equal("Resell", payload.Outcome);
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
            Task.FromResult<DispositionExecutionResultDto?>(new DispositionExecutionResultDto(
                Guid.NewGuid(),
                "Completed",
                null,
                "Resell"));

        public Task<DispositionExecutionTraceDto?> GetDispositionTraceAsync(
            Guid workflowInstanceId,
            CancellationToken cancellationToken) =>
            Task.FromResult<DispositionExecutionTraceDto?>(null);

        public Task<SopExecutionViewDto?> AdvanceSopSessionAsync(Guid sessionId, AdvanceSopStepRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<SopExecutionViewDto?>(null);

        public Task ProxySseAsync(Guid sessionId, HttpResponse response, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
