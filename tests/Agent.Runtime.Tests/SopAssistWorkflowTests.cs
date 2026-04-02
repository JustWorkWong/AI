using Agent.Runtime.Workflows;

namespace Agent.Runtime.Tests;

public sealed class SopAssistWorkflowTests
{
    [Fact]
    public async Task Workflow_should_return_manual_review_when_no_evidence_found()
    {
        var fixture = new SopAssistFixture(hasEvidence: false);

        var result = await fixture.RunAsync();

        Assert.Equal("ManualReviewRequired", result.Status);
    }

    private sealed class SopAssistFixture(bool hasEvidence)
    {
        public RuntimeContext Context { get; } = new();

        public Task<SopAssistResult> RunAsync()
        {
            Context.RegisterTool(
                "SearchSopCandidatesTool",
                (_ , _) => Task.FromResult<object?>(new[] { "DOC-001" }));

            Context.RegisterTool(
                "RetrieveSopChunksTool",
                (_ , _) => Task.FromResult<object?>(hasEvidence ? new[] { "chunk-1" } : Array.Empty<string>()));

            Context.RegisterTool(
                "RankSopEvidenceTool",
                (_ , _) => Task.FromResult<object?>(hasEvidence ? new[] { "evidence-1" } : Array.Empty<string>()));

            Context.RegisterGenerator(
                "sop-assist",
                (_ , _) => Task.FromResult<object?>(new SopAssistResponse("Step guidance")));

            var workflow = new SopAssistWorkflow();
            return workflow.RunStepAsync(new SopAssistInput(Guid.NewGuid(), "RETURNS", "INSPECT"), Context);
        }
    }
}
