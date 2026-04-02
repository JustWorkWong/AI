using Agent.Runtime.Tools;

namespace Agent.Runtime.Workflows;

public sealed class ReturnDispositionWorkflow
{
    public async Task<WorkflowResult> RunAsync(
        ReturnDispositionInput input,
        RuntimeContext context,
        CancellationToken cancellationToken = default)
    {
        var returnOrder = await context.InvokeAsync<ReturnOrderSnapshot>(
            GetReturnOrderTool.Name,
            new GetReturnOrderToolInput(input.ReturnOrderId),
            cancellationToken);

        var sop = await context.InvokeAsync<string[]>(
            SearchSopTool.Name,
            new SearchReturnSopToolInput(input.ReturnOrderId, returnOrder),
            cancellationToken);

        var cases = await context.InvokeAsync<string[]>(
            SearchHistoricalCasesTool.Name,
            new SearchHistoricalCasesToolInput(input.ReturnOrderId, returnOrder),
            cancellationToken);

        var suggestion = await context.GenerateAsync<DispositionSuggestion>(
            "return-disposition",
            new
            {
                input.ReturnOrderId,
                ReturnOrder = returnOrder,
                Sop = sop,
                Cases = cases
            },
            cancellationToken);

        if (suggestion.RequiresApproval)
        {
            var approval = await context.InvokeAsync<ApprovalReference>(
                RequestDispositionApprovalTool.Name,
                new RequestDispositionApprovalToolInput(input.ReturnOrderId, suggestion.Outcome),
                cancellationToken);

            await context.CreateCheckpointAsync("approval", approval.ReferenceId, cancellationToken);
            return WorkflowResult.WaitingForApproval(approval.ReferenceId);
        }

        await context.InvokeAsync(
            ApplyDispositionDecisionTool.Name,
            new ApplyDispositionDecisionToolInput(input.ReturnOrderId, suggestion.Outcome, input.IdempotencyKey),
            cancellationToken);

        return WorkflowResult.Completed(suggestion.Outcome);
    }
}
