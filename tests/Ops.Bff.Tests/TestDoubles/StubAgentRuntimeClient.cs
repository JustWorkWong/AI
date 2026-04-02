using Microsoft.AspNetCore.Http;
using Ops.Bff.Clients;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Ops.Bff.Tests.TestDoubles;

internal sealed class StubAgentRuntimeClient : IAgentRuntimeClient
{
    public int FailureCount { get; init; }

    public Func<Guid, CancellationToken, Task<DispositionSuggestionDto?>> GetDispositionSuggestionAsyncHandler { get; init; } =
        static (_, _) => Task.FromResult<DispositionSuggestionDto?>(null);

    public Func<Guid, ExecuteDispositionRequest, CancellationToken, Task<DispositionExecutionResultDto?>> ExecuteDispositionAsyncHandler { get; init; } =
        static (_, _, _) => Task.FromResult<DispositionExecutionResultDto?>(null);

    public Func<Guid, CancellationToken, Task<DispositionExecutionTraceDto?>> GetDispositionTraceAsyncHandler { get; init; } =
        static (_, _) => Task.FromResult<DispositionExecutionTraceDto?>(null);

    public Func<Guid, ApprovalDecisionRequest, CancellationToken, Task<DispositionExecutionResultDto?>> DecideDispositionApprovalAsyncHandler { get; init; } =
        static (_, _, _) => Task.FromResult<DispositionExecutionResultDto?>(null);

    public Func<Guid, AdvanceSopStepRequest, CancellationToken, Task<SopExecutionViewDto?>> AdvanceSopSessionAsyncHandler { get; init; } =
        static (_, _, _) => Task.FromResult<SopExecutionViewDto?>(null);

    public Func<Guid, HttpResponse, CancellationToken, Task> ProxySseAsyncHandler { get; init; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<int> GetFailureCountAsync(CancellationToken cancellationToken) =>
        Task.FromResult(FailureCount);

    public Task<DispositionSuggestionDto?> GetDispositionSuggestionAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
        GetDispositionSuggestionAsyncHandler(returnOrderId, cancellationToken);

    public Task<DispositionExecutionResultDto?> ExecuteDispositionAsync(
        Guid returnOrderId,
        ExecuteDispositionRequest request,
        CancellationToken cancellationToken) =>
        ExecuteDispositionAsyncHandler(returnOrderId, request, cancellationToken);

    public Task<DispositionExecutionTraceDto?> GetDispositionTraceAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken) =>
        GetDispositionTraceAsyncHandler(workflowInstanceId, cancellationToken);

    public Task<DispositionExecutionResultDto?> DecideDispositionApprovalAsync(
        Guid workflowInstanceId,
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken) =>
        DecideDispositionApprovalAsyncHandler(workflowInstanceId, request, cancellationToken);

    public Task<SopExecutionViewDto?> AdvanceSopSessionAsync(
        Guid sessionId,
        AdvanceSopStepRequest request,
        CancellationToken cancellationToken) =>
        AdvanceSopSessionAsyncHandler(sessionId, request, cancellationToken);

    public Task ProxySseAsync(Guid sessionId, HttpResponse response, CancellationToken cancellationToken) =>
        ProxySseAsyncHandler(sessionId, response, cancellationToken);
}
