using Agent.Runtime.Clients;
using Agent.Runtime.Services;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Agent.Runtime.Tests;

public sealed class ReturnDispositionAdvisorTests
{
    [Fact]
    public async Task Get_suggestion_should_include_sop_and_historical_case_citations()
    {
        var returnOrderId = Guid.NewGuid();
        var advisor = new ReturnDispositionAdvisor(new StubDomainKnowledgeClient(returnOrderId));

        var result = await advisor.GetSuggestionAsync(returnOrderId, CancellationToken.None);

        Assert.Equal(returnOrderId, result.ReturnOrderId);
        Assert.Equal("Scrap", result.SuggestedOutcome);
        Assert.Contains(result.Citations, x => x.SourceType == "sop");
        Assert.Contains(result.Citations, x => x.SourceType == "historical-case");
    }

    private sealed class StubDomainKnowledgeClient(Guid returnOrderId) : IDomainKnowledgeClient
    {
        public Task<ReturnOrderDto?> GetReturnOrderAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<ReturnOrderDto?>(new ReturnOrderDto(returnOrderId, "RET-001", "Broken", "Open", "Screen cracked"));

        public Task<IReadOnlyList<HistoricalCaseDto>> GetHistoricalCasesAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HistoricalCaseDto>>([
                new HistoricalCaseDto(Guid.NewGuid(), "Broken", "Scrap")
            ]);

        public Task<IReadOnlyList<SopCandidateDto>> SearchSopCandidatesAsync(
            string operationCode,
            string stepCode,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SopCandidateDto>>([
                new SopCandidateDto(Guid.NewGuid(), "SOP-RET-001", "v1", "退货处置")
            ]);

        public Task<IReadOnlyList<SopChunkDto>> RetrieveSopChunksAsync(
            RetrieveSopChunksQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SopChunkDto>>([
                new SopChunkDto(Guid.NewGuid(), query.CandidateDocumentIds[0], "SOP-RET-001", "v1", query.StepCode, "Broken items must be scrapped and escalated.")
            ]);
    }
}
