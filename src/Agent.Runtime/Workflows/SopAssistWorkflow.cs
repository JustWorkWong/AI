using Agent.Runtime.Tools;

namespace Agent.Runtime.Workflows;

public sealed class SopAssistWorkflow
{
    public async Task<SopAssistResult> RunStepAsync(
        SopAssistInput input,
        RuntimeContext context,
        CancellationToken cancellationToken = default)
    {
        var candidates = await context.InvokeAsync<string[]>(
            SearchSopCandidatesTool.Name,
            new
            {
                input.OperationCode,
                input.StepCode
            },
            cancellationToken);

        var chunks = await context.InvokeAsync<string[]>(
            RetrieveSopChunksTool.Name,
            new
            {
                input.OperationCode,
                input.StepCode,
                Candidates = candidates
            },
            cancellationToken);

        var evidence = await context.InvokeAsync<string[]>(
            RankSopEvidenceTool.Name,
            new
            {
                input.OperationCode,
                input.StepCode,
                Chunks = chunks
            },
            cancellationToken);

        if (evidence.Length == 0)
        {
            return SopAssistResult.ManualReviewRequired();
        }

        var response = await context.GenerateAsync<SopAssistResponse>(
            "sop-assist",
            new
            {
                input.OperationCode,
                input.StepCode,
                Evidence = evidence
            },
            cancellationToken);

        await context.CreateCheckpointAsync("step", input.SessionId, cancellationToken);
        return SopAssistResult.Success(response, evidence);
    }
}
