using Ops.Bff.Clients;
using Ops.Bff.Queries;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;
using Shared.Contracts.Approvals;
using Shared.Contracts.Common;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace Ops.Bff.Tests;

public sealed class ReturnWorkbenchQueryServiceTests
{
    [Fact]
    public async Task Get_view_should_return_unavailable_suggestion_when_runtime_throws()
    {
        var service = new ReturnWorkbenchQueryService(
            new StubDomainServiceClient(),
            new ThrowingAgentRuntimeClient());

        var result = await service.GetViewAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Unavailable", result!.Suggestion.ApprovalStatus);
        Assert.Equal("PendingInspection", result.Order.Status);
    }
}

internal sealed class StubDomainServiceClient : IDomainServiceClient
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

internal sealed class ThrowingAgentRuntimeClient : IAgentRuntimeClient
{
    public Task<int> GetFailureCountAsync(CancellationToken cancellationToken) =>
        Task.FromResult(0);

    public Task<DispositionSuggestionDto?> GetDispositionSuggestionAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
        throw new HttpRequestException("runtime unavailable");

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
