using Agent.Runtime.Clients;
using Agent.Runtime.Services;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Agent.Runtime.Tests;

public sealed class SopAssistServiceTests
{
    [Fact]
    public async Task Advance_should_return_ranked_citations_for_matching_step()
    {
        var sessionId = Guid.NewGuid();
        var service = new SopAssistService(new StubDomainKnowledgeClient());

        var result = await service.AdvanceAsync(
            sessionId,
            new AdvanceSopStepRequest("INSPECT", "screen cracked"),
            CancellationToken.None);

        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal("INSPECT", result.CurrentStepCode);
        Assert.NotEmpty(result.Citations);
        Assert.True(result.RequiresAcknowledgement);
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
                new SopChunkDto(Guid.NewGuid(), query.CandidateDocumentIds[0], "SOP-RET-101", "v3", "PACK", "Verify package seals.")
            ]);
    }
}
