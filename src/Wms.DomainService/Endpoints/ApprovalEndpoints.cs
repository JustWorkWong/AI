using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Approvals;
using Wms.DomainService.Approvals;
using Wms.DomainService.Persistence;

namespace Wms.DomainService.Endpoints;

public static class ApprovalEndpoints
{
    public static IEndpointRouteBuilder MapApprovalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/internal/approvals/{approvalTaskId:guid}", async (
            Guid approvalTaskId,
            WmsDbContext db,
            CancellationToken cancellationToken) =>
        {
            var task = await db.ApprovalTasks
                .Where(x => x.Id == approvalTaskId)
                .Select(x => new ApprovalTaskDto(
                    x.Id,
                    x.ApprovalType,
                    x.Status,
                    x.AggregateId))
                .SingleOrDefaultAsync(cancellationToken);

            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        endpoints.MapPost("/internal/approvals/{approvalTaskId:guid}/actions", async (
            Guid approvalTaskId,
            ApprovalDecisionRequest request,
            WmsDbContext db,
            CancellationToken cancellationToken) =>
        {
            var task = await db.ApprovalTasks.SingleOrDefaultAsync(
                x => x.Id == approvalTaskId,
                cancellationToken);

            if (task is null)
            {
                return Results.NotFound();
            }

            db.ApprovalActions.Add(new ApprovalAction(
                Guid.NewGuid(),
                approvalTaskId,
                request.Action,
                request.Actor));

            if (string.Equals(request.Action, "Approve", StringComparison.OrdinalIgnoreCase))
            {
                task.MarkApproved();
            }
            else if (string.Equals(request.Action, "Reject", StringComparison.OrdinalIgnoreCase))
            {
                task.MarkRejected();
            }
            else
            {
                return Results.BadRequest(new { error = "Unsupported approval action." });
            }

            await db.SaveChangesAsync(cancellationToken);
            return Results.Accepted();
        });

        return endpoints;
    }
}
