using Agent.Runtime.Clients;
using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;
using Agent.Runtime.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Approvals;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Agent.Runtime.Tests;

public sealed class ReturnDispositionExecutorTests
{
    [Fact]
    public async Task Execute_should_request_approval_for_high_risk_disposition()
    {
        await using var db = CreateDbContext();
        var executor = new ReturnDispositionExecutor(
            new StubDomainKnowledgeClient("Broken"),
            new StubDomainDispositionClient(),
            new ToolLoggingMiddleware(new EfToolInvocationStore(db)),
            db);

        var result = await executor.ExecuteAsync(
            Guid.NewGuid(),
            new ExecuteDispositionRequest("idem-approval"),
            CancellationToken.None);

        Assert.Equal("WaitingForApproval", result.Status);
        Assert.NotNull(result.ApprovalReferenceId);

        var workflow = Assert.Single(db.WorkflowInstances);
        Assert.Equal(WorkflowInstanceStatus.WaitingApproval, workflow.Status);
        Assert.Equal(4, db.ToolInvocations.Count());
        Assert.Single(db.WorkflowCheckpoints);
    }

    [Fact]
    public async Task Execute_should_apply_disposition_without_approval_for_low_risk_disposition()
    {
        await using var db = CreateDbContext();
        var dispositionClient = new StubDomainDispositionClient();
        var executor = new ReturnDispositionExecutor(
            new StubDomainKnowledgeClient("Sealed"),
            dispositionClient,
            new ToolLoggingMiddleware(new EfToolInvocationStore(db)),
            db);

        var result = await executor.ExecuteAsync(
            Guid.NewGuid(),
            new ExecuteDispositionRequest("idem-complete"),
            CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.Equal("Resell", result.Outcome);
        Assert.Single(dispositionClient.AppliedCommands);

        var workflow = Assert.Single(db.WorkflowInstances);
        Assert.Equal(WorkflowInstanceStatus.Completed, workflow.Status);
        Assert.Equal(4, db.ToolInvocations.Count());
        Assert.Empty(db.WorkflowCheckpoints);
    }

    private static AgentRuntimeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AgentRuntimeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AgentRuntimeDbContext(options);
    }

    private sealed class StubDomainKnowledgeClient(string qualityState) : IDomainKnowledgeClient
    {
        public Task<ReturnOrderDto?> GetReturnOrderAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<ReturnOrderDto?>(new ReturnOrderDto(returnOrderId, "RET-001", qualityState, "Open", "notes"));

        public Task<IReadOnlyList<HistoricalCaseDto>> GetHistoricalCasesAsync(Guid returnOrderId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HistoricalCaseDto>>([
                new HistoricalCaseDto(Guid.NewGuid(), qualityState, qualityState == "Broken" ? "Scrap" : "Resell")
            ]);

        public Task<IReadOnlyList<SopCandidateDto>> SearchSopCandidatesAsync(string operationCode, string stepCode, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SopCandidateDto>>([
                new SopCandidateDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "SOP-RET-201", "v1", "处置规范")
            ]);

        public Task<IReadOnlyList<SopChunkDto>> RetrieveSopChunksAsync(RetrieveSopChunksQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SopChunkDto>>([
                new SopChunkDto(Guid.NewGuid(), Guid.Parse(query.CandidateDocumentIds[0].ToString()), "SOP-RET-201", "v1", query.StepCode, "Disposition guidance")
            ]);
    }

    private sealed class StubDomainDispositionClient : IDomainDispositionClient
    {
        public List<ApplyDispositionCommand> AppliedCommands { get; } = [];

        public Task<Guid> RequestApprovalAsync(RequestDispositionApproval command, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task DecideApprovalAsync(Guid approvalTaskId, ApprovalDecisionCommand command, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ApplyDispositionAsync(ApplyDispositionCommand command, CancellationToken cancellationToken)
        {
            AppliedCommands.Add(command);
            return Task.CompletedTask;
        }
    }
}
