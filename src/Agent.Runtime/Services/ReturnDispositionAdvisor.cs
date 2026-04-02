using Agent.Runtime.Clients;
using Shared.Contracts.Common;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Agent.Runtime.Services;

public sealed class ReturnDispositionAdvisor(IDomainKnowledgeClient domainKnowledgeClient)
{
    public async Task<DispositionSuggestionDto> GetSuggestionAsync(
        Guid returnOrderId,
        CancellationToken cancellationToken)
    {
        var order = await domainKnowledgeClient.GetReturnOrderAsync(returnOrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Return order '{returnOrderId}' was not found.");

        var historicalCases = await domainKnowledgeClient.GetHistoricalCasesAsync(returnOrderId, cancellationToken);
        var candidates = await domainKnowledgeClient.SearchSopCandidatesAsync("RETURNS", "DISPOSITION", cancellationToken);
        var chunks = await domainKnowledgeClient.RetrieveSopChunksAsync(
            new RetrieveSopChunksQuery("RETURNS", "DISPOSITION", candidates.Select(x => x.DocumentId).ToArray()),
            cancellationToken);

        var citations = BuildCitations(historicalCases, chunks);
        var outcome = order.QualityState.Equals("Broken", StringComparison.OrdinalIgnoreCase)
            ? "Scrap"
            : "Resell";
        var riskLevel = outcome == "Scrap" ? "High" : "Low";
        var approvalStatus = riskLevel == "High" ? "Pending" : "NotRequired";

        return new DispositionSuggestionDto(
            returnOrderId,
            outcome,
            riskLevel,
            citations,
            approvalStatus);
    }

    private static IReadOnlyList<CitationDto> BuildCitations(
        IReadOnlyList<HistoricalCaseDto> historicalCases,
        IReadOnlyList<SopChunkDto> chunks)
    {
        var citations = new List<CitationDto>();

        citations.AddRange(chunks
            .Take(2)
            .Select(x => new CitationDto("sop", x.DocumentCode, x.Version, x.Content)));

        citations.AddRange(historicalCases
            .Take(1)
            .Select(x => new CitationDto("historical-case", x.CaseId.ToString("N"), "snapshot", $"{x.Condition} -> {x.Outcome}")));

        return citations;
    }
}
