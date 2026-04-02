using Agent.Runtime.Workflows;

namespace Agent.Runtime.Tests;

public sealed class ReturnDispositionWorkflowTests
{
    [Fact]
    public async Task Workflow_should_pause_when_approval_is_required()
    {
        var fixture = new ReturnDispositionFixture(approvalRequired: true);

        var result = await fixture.RunAsync();

        Assert.Equal("WaitingForApproval", result.Status);
        Assert.NotNull(result.ApprovalReferenceId);
        Assert.Contains(
            fixture.Context.Checkpoints,
            x => x.CheckpointType == "approval" && x.Value is Guid);
    }

    private sealed class ReturnDispositionFixture(bool approvalRequired)
    {
        public RuntimeContext Context { get; } = new();

        public Task<WorkflowResult> RunAsync()
        {
            Context.RegisterTool(
                "GetReturnOrderTool",
                (_ , _) => Task.FromResult<object?>(new ReturnOrderSnapshot(Guid.NewGuid(), "Broken")));

            Context.RegisterTool(
                "SearchSopTool",
                (_ , _) => Task.FromResult<object?>(new[] { "SOP-RET-001" }));

            Context.RegisterTool(
                "SearchHistoricalCasesTool",
                (_ , _) => Task.FromResult<object?>(new[] { "CASE-001" }));

            Context.RegisterTool(
                "RequestDispositionApprovalTool",
                (_ , _) => Task.FromResult<object?>(new ApprovalReference(Guid.NewGuid())));

            Context.RegisterTool(
                "ApplyDispositionDecisionTool",
                (_ , _) => Task.FromResult<object?>(new { Accepted = true }));

            Context.RegisterGenerator(
                "return-disposition",
                (_ , _) => Task.FromResult<object?>(new DispositionSuggestion(approvalRequired, "Scrap")));

            var workflow = new ReturnDispositionWorkflow();
            return workflow.RunAsync(new ReturnDispositionInput(Guid.NewGuid(), "idem-001"), Context);
        }
    }
}
