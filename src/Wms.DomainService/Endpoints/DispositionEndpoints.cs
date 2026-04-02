using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Returns;
using Wms.DomainService.Approvals;
using Wms.DomainService.Commands;
using Wms.DomainService.Persistence;
using Wms.DomainService.Returns;

namespace Wms.DomainService.Endpoints;

public static class DispositionEndpoints
{
    public static IEndpointRouteBuilder MapDispositionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/internal/dispositions/request-approval", async (
            RequestDispositionApproval command,
            WmsDbContext db,
            CancellationToken cancellationToken) =>
        {
            var task = new ApprovalTask(Guid.NewGuid(), "Disposition", command.ReturnOrderId);

            db.ApprovalTasks.Add(task);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Accepted($"/internal/approvals/{task.Id}");
        });

        endpoints.MapPost("/internal/dispositions/apply", async (
            ApplyDispositionCommand command,
            WmsDbContext db,
            CancellationToken cancellationToken) =>
        {
            var seen = await db.CommandDeduplications
                .AnyAsync(x => x.IdempotencyKey == command.IdempotencyKey, cancellationToken);

            if (!seen)
            {
                db.CommandDeduplications.Add(
                    new CommandDeduplication(command.IdempotencyKey, "ApplyDisposition"));
                db.DispositionDecisions.Add(
                    new DispositionDecision(Guid.NewGuid(), command.ReturnOrderId, command.Outcome));

                var order = await db.ReturnOrders.SingleOrDefaultAsync(
                    x => x.Id == command.ReturnOrderId,
                    cancellationToken);

                if (order is null)
                {
                    order = new ReturnOrder(command.ReturnOrderId, $"RET-{command.ReturnOrderId:N}"[..12]);
                    db.ReturnOrders.Add(order);
                }

                order.MarkDisposed();
                await db.SaveChangesAsync(cancellationToken);
            }

            return Results.Accepted();
        });

        return endpoints;
    }
}
