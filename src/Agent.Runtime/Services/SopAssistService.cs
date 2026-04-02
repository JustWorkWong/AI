using Agent.Runtime.Clients;
using Shared.Contracts.Common;
using Shared.Contracts.Sop;

namespace Agent.Runtime.Services;

public sealed class SopAssistService(IDomainKnowledgeClient domainKnowledgeClient)
{
    public async Task<SopExecutionViewDto> AdvanceAsync(
        Guid sessionId,
        AdvanceSopStepRequest request,
        CancellationToken cancellationToken)
    {
        var candidates = await domainKnowledgeClient.SearchSopCandidatesAsync(
            "RETURNS",
            request.StepCode,
            cancellationToken);

        var chunks = await domainKnowledgeClient.RetrieveSopChunksAsync(
            new RetrieveSopChunksQuery(
                "RETURNS",
                request.StepCode,
                candidates.Select(x => x.DocumentId).ToArray()),
            cancellationToken);

        var citations = RankEvidence(chunks, request.UserInput);

        return new SopExecutionViewDto(
            sessionId,
            "RETURNS",
            request.StepCode,
            citations,
            citations.Count > 0);
    }

    private static IReadOnlyList<CitationDto> RankEvidence(
        IReadOnlyList<SopChunkDto> chunks,
        string userInput)
    {
        var tokens = userInput
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToArray();

        return chunks
            .Select(chunk => new
            {
                Chunk = chunk,
                Score = Score(chunk.Content, tokens)
            })
            .Where(x => x.Score > 0 || tokens.Length == 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Chunk.DocumentCode)
            .Select(x => new CitationDto("sop", x.Chunk.DocumentCode, x.Chunk.Version, x.Chunk.Content))
            .Take(3)
            .ToArray();
    }

    private static int Score(string content, IReadOnlyList<string> tokens)
    {
        if (tokens.Count == 0)
        {
            return 1;
        }

        var haystack = content.ToLowerInvariant();
        return tokens.Count(haystack.Contains);
    }
}
