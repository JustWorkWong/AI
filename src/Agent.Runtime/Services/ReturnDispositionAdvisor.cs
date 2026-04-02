using Agent.Runtime.Clients;
using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;
using Shared.Contracts.Common;
using Shared.Contracts.Returns;
using Shared.Contracts.Sop;

namespace Agent.Runtime.Services;

public sealed class ReturnDispositionAdvisor(
    IDomainKnowledgeClient domainKnowledgeClient,
    ToolLoggingMiddleware toolLoggingMiddleware,
    AgentRuntimeDbContext dbContext)
{
    public async Task<DispositionSuggestionDto> GetSuggestionAsync(
        Guid returnOrderId,
        CancellationToken cancellationToken)
    {
        var workflowInstance = new WorkflowInstance
        {
            SessionId = returnOrderId,
            WorkflowCode = "return-disposition-read",
            Status = WorkflowInstanceStatus.Running
        };

        dbContext.WorkflowInstances.Add(workflowInstance);
        await dbContext.SaveChangesAsync(cancellationToken);

        var traceId = Guid.NewGuid().ToString("N");

        try
        {
            var order = await toolLoggingMiddleware.ExecuteAsync(
                    Tools.GetReturnOrderTool.Name,
                    traceId,
                    $"{{\"returnOrderId\":\"{returnOrderId}\"}}",
                    ct => domainKnowledgeClient.GetReturnOrderAsync(returnOrderId, ct),
                    workflowInstance.Id,
                    cancellationToken)
            ?? throw new InvalidOperationException($"Return order '{returnOrderId}' was not found.");

            var historicalCases = await toolLoggingMiddleware.ExecuteAsync(
                Tools.SearchHistoricalCasesTool.Name,
                traceId,
                $"{{\"returnOrderId\":\"{returnOrderId}\",\"qualityState\":\"{order.QualityState}\"}}",
                ct => domainKnowledgeClient.GetHistoricalCasesAsync(returnOrderId, ct),
                workflowInstance.Id,
                cancellationToken);

            var chunks = await toolLoggingMiddleware.ExecuteAsync(
                Tools.SearchSopTool.Name,
                traceId,
                $"{{\"operationCode\":\"RETURNS\",\"stepCode\":\"DISPOSITION\"}}",
                async ct =>
                {
                    var candidates = await domainKnowledgeClient.SearchSopCandidatesAsync("RETURNS", "DISPOSITION", ct);
                    return await domainKnowledgeClient.RetrieveSopChunksAsync(
                        new RetrieveSopChunksQuery("RETURNS", "DISPOSITION", candidates.Select(x => x.DocumentId).ToArray()),
                        ct);
                },
                workflowInstance.Id,
                cancellationToken);

            var citations = BuildCitations(historicalCases, chunks);
            var outcome = order.QualityState.Equals("Broken", StringComparison.OrdinalIgnoreCase)
                ? "Scrap"
                : "Resell";
            var riskLevel = outcome == "Scrap" ? "High" : "Low";
            var approvalStatus = riskLevel == "High" ? "Pending" : "NotRequired";

            workflowInstance.Status = WorkflowInstanceStatus.Completed;
            workflowInstance.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            return new DispositionSuggestionDto(
                returnOrderId,
                outcome,
                riskLevel,
                citations,
                approvalStatus);
        }
        catch
        {
            workflowInstance.Status = WorkflowInstanceStatus.Failed;
            workflowInstance.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
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
