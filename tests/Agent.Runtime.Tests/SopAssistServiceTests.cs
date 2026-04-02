using Agent.Runtime.Clients;
using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;
using Agent.Runtime.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Agent.Runtime.Tests;

public sealed class SopAssistServiceTests
{
    [Fact]
    public async Task Advance_should_return_ranked_citations_for_matching_step()
    {
        var sessionId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var service = new SopAssistService(
            new StubDomainKnowledgeClient(),
            new ToolLoggingMiddleware(new EfToolInvocationStore(db)),
            db);

        var result = await service.AdvanceAsync(
            sessionId,
            new AdvanceSopStepRequest("INSPECT", "screen cracked"),
            CancellationToken.None);

        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal("INSPECT", result.CurrentStepCode);
        Assert.NotEmpty(result.Citations);
        Assert.True(result.RequiresAcknowledgement);
    }

    [Fact]
    public async Task Advance_should_persist_workflow_instance_checkpoint_and_tool_logs()
    {
        var sessionId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var service = new SopAssistService(
            new StubDomainKnowledgeClient(),
            new ToolLoggingMiddleware(new EfToolInvocationStore(db)),
            db);

        _ = await service.AdvanceAsync(
            sessionId,
            new AdvanceSopStepRequest("INSPECT", "screen cracked"),
            CancellationToken.None);

        var workflow = Assert.Single(db.WorkflowInstances);
        Assert.Equal("sop-assist", workflow.WorkflowCode);
        Assert.Equal(WorkflowInstanceStatus.Completed, workflow.Status);
        Assert.Single(db.WorkflowCheckpoints);
        Assert.Equal(3, db.ToolInvocations.Count());
    }

    [Fact]
    public async Task Advance_should_fall_back_to_top_sop_citations_when_user_input_has_no_keyword_match()
    {
        var sessionId = Guid.NewGuid();
        await using var db = CreateDbContext();
        var service = new SopAssistService(
            new StubDomainKnowledgeClient(),
            new ToolLoggingMiddleware(new EfToolInvocationStore(db)),
            db);

        var result = await service.AdvanceAsync(
            sessionId,
            new AdvanceSopStepRequest("STEP-02", "confirmed"),
            CancellationToken.None);

        Assert.NotEmpty(result.Citations);
        Assert.True(result.RequiresAcknowledgement);
    }

    private static AgentRuntimeDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AgentRuntimeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AgentRuntimeDbContext(options);
    }

    private sealed class StubDomainKnowledgeClient : IDomainKnowledgeClient
    {
        public Task<ReturnOrderDto?> GetReturnOrderAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ReturnOrderDto?>(null);

        public Task<IReadOnlyList<HistoricalCaseDto>> GetHistoricalCasesAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HistoricalCaseDto>>([]);

        public Task<IReadOnlyList<SopCandidateDto>> SearchSopCandidatesAsync(
            string operationCode,
            string stepCode,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SopCandidateDto>>([
                new SopCandidateDto(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "SOP-RET-101", "v3", "退货质检")
            ]);

        public Task<IReadOnlyList<SopChunkDto>> RetrieveSopChunksAsync(
            RetrieveSopChunksQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SopChunkDto>>([
                new SopChunkDto(Guid.NewGuid(), query.CandidateDocumentIds[0], "SOP-RET-101", "v3", "INSPECT", "Inspect the cracked screen before approving disposal."),
                new SopChunkDto(Guid.NewGuid(), query.CandidateDocumentIds[0], "SOP-RET-101", "v3", "STEP-02", "确认质检结论并记录现场人员确认结果。"),
                new SopChunkDto(Guid.NewGuid(), query.CandidateDocumentIds[0], "SOP-RET-101", "v3", "PACK", "Verify package seals.")
            ]);
    }
}
