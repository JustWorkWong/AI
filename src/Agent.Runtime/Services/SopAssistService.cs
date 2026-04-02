using Agent.Runtime.Clients;
using Agent.Runtime.Observability;
using Agent.Runtime.Persistence;
using Agent.Runtime.Tools;
using Agent.Runtime.Workflows;
using Shared.Contracts.Common;
using Shared.Contracts.Sop;
using System.Text.Json;

namespace Agent.Runtime.Services;

public sealed class SopAssistService(
    IDomainKnowledgeClient domainKnowledgeClient,
    ToolLoggingMiddleware toolLoggingMiddleware,
    AgentRuntimeDbContext dbContext)
{
    public async Task<SopExecutionViewDto> AdvanceAsync(
        Guid sessionId,
        AdvanceSopStepRequest request,
        CancellationToken cancellationToken)
    {
        var workflowInstance = new WorkflowInstance
        {
            SessionId = sessionId,
            WorkflowCode = "sop-assist",
            Status = WorkflowInstanceStatus.Running
        };

        dbContext.WorkflowInstances.Add(workflowInstance);
        await dbContext.SaveChangesAsync(cancellationToken);

        var traceId = Guid.NewGuid().ToString("N");
        var context = BuildRuntimeContext(workflowInstance.Id, traceId, request.UserInput);

        try
        {
            var workflow = new SopAssistWorkflow();
            var result = await workflow.RunStepAsync(
                new SopAssistInput(sessionId, "RETURNS", request.StepCode),
                context,
                cancellationToken);

            await PersistCheckpointsAsync(workflowInstance.Id, context.Checkpoints, cancellationToken);

            workflowInstance.Status = result.Status == "ManualReviewRequired"
                ? WorkflowInstanceStatus.Completed
                : WorkflowInstanceStatus.Completed;
            workflowInstance.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            if (result.Payload is not SopAssistPayload payload)
            {
                return new SopExecutionViewDto(sessionId, "RETURNS", request.StepCode, [], false);
            }

            var citations = payload.Evidence
                .Select((evidence, index) => new CitationDto(
                    "sop",
                    $"{workflowInstance.Id:N}-{index + 1}",
                    "runtime",
                    evidence))
                .ToArray();

            return new SopExecutionViewDto(
                sessionId,
                "RETURNS",
                request.StepCode,
                citations,
                citations.Length > 0);
        }
        catch
        {
            workflowInstance.Status = WorkflowInstanceStatus.Failed;
            workflowInstance.CompletedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private RuntimeContext BuildRuntimeContext(Guid workflowInstanceId, string traceId, string userInput)
    {
        var context = new RuntimeContext();

        context.RegisterTool(
            SearchSopCandidatesTool.Name,
            async (input, cancellationToken) =>
            {
                var request = (SearchSopCandidatesToolInput)input;
                var candidates = await toolLoggingMiddleware.ExecuteAsync(
                    SearchSopCandidatesTool.Name,
                    traceId,
                    JsonSerializer.Serialize(request),
                    ct => domainKnowledgeClient.SearchSopCandidatesAsync(request.OperationCode, request.StepCode, ct),
                    workflowInstanceId,
                    cancellationToken);

                return candidates.Select(x => x.DocumentId.ToString()).ToArray();
            });

        context.RegisterTool(
            RetrieveSopChunksTool.Name,
            async (input, cancellationToken) =>
            {
                var request = (RetrieveSopChunksToolInput)input;
                var documentIds = request.CandidateDocumentIds
                    .Select(Guid.Parse)
                    .ToArray();

                var chunks = await toolLoggingMiddleware.ExecuteAsync(
                    RetrieveSopChunksTool.Name,
                    traceId,
                    JsonSerializer.Serialize(request),
                    ct => domainKnowledgeClient.RetrieveSopChunksAsync(
                        new RetrieveSopChunksQuery(request.OperationCode, request.StepCode, documentIds),
                        ct),
                    workflowInstanceId,
                    cancellationToken);

                return chunks.Select(x => x.Content).ToArray();
            });

        context.RegisterTool(
            RankSopEvidenceTool.Name,
            async (input, cancellationToken) =>
            {
                var request = (RankSopEvidenceToolInput)input;
                var evidence = await toolLoggingMiddleware.ExecuteAsync(
                    RankSopEvidenceTool.Name,
                    traceId,
                    JsonSerializer.Serialize(request),
                    _ => Task.FromResult(RankEvidence(request.Chunks, userInput).Select(x => x.Snippet).ToArray()),
                    workflowInstanceId,
                    cancellationToken);

                return evidence;
            });

        context.RegisterGenerator(
            "sop-assist",
            (input, _) =>
            {
                var request = (dynamic)input;
                var evidence = (string[])request.Evidence;
                var response = new SopAssistResponse($"Found {evidence.Length} SOP evidence items for {request.StepCode}.");
                return Task.FromResult<object?>(response);
            });

        return context;
    }

    private async Task PersistCheckpointsAsync(
        Guid workflowInstanceId,
        IReadOnlyList<RuntimeCheckpoint> checkpoints,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < checkpoints.Count; index++)
        {
            var checkpoint = checkpoints[index];
            dbContext.WorkflowCheckpoints.Add(new WorkflowCheckpoint
            {
                WorkflowInstanceId = workflowInstanceId,
                Superstep = index + 1,
                CheckpointType = checkpoint.CheckpointType,
                StateJson = JsonSerializer.Serialize(checkpoint.Value)
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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

    private static IReadOnlyList<CitationDto> RankEvidence(
        IReadOnlyList<string> chunks,
        string userInput)
    {
        var typedChunks = chunks
            .Select((content, index) => new SopChunkDto(
                Guid.NewGuid(),
                Guid.Empty,
                $"runtime-{index + 1}",
                "runtime",
                "STEP",
                content))
            .ToArray();

        return RankEvidence(typedChunks, userInput);
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
