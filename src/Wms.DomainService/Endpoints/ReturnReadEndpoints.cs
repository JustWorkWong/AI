using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Returns;
using Wms.DomainService.Persistence;

namespace Wms.DomainService.Endpoints;

public static class ReturnReadEndpoints
{
    public static IEndpointRouteBuilder MapReturnReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/internal/returns/{returnOrderId:guid}", async (
            Guid returnOrderId,
            HttpContext httpContext,
            WmsDbContext db,
            CancellationToken cancellationToken) =>
        {
            var order = await db.ReturnOrders.SingleOrDefaultAsync(
                x => x.Id == returnOrderId,
                cancellationToken);

            if (order is null)
            {
                return Program.CreateProblemResult(
                    httpContext,
                    StatusCodes.Status404NotFound,
                    "Return order not found",
                    $"Return order '{returnOrderId}' does not exist.");
            }

            var latestInspection = await db.QualityInspections
                .Where(x => x.ReturnOrderId == returnOrderId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            return Results.Ok(new ReturnOrderDto(
                order.Id,
                order.ReturnNo,
                latestInspection?.Condition ?? "Unknown",
                order.Status,
                latestInspection?.Notes ?? string.Empty));
        });

        endpoints.MapGet("/internal/returns/{returnOrderId:guid}/historical-cases", async (
            Guid returnOrderId,
            HttpContext httpContext,
            WmsDbContext db,
            CancellationToken cancellationToken) =>
        {
            var orderExists = await db.ReturnOrders
                .AnyAsync(x => x.Id == returnOrderId, cancellationToken);

            if (!orderExists)
            {
                return Program.CreateProblemResult(
                    httpContext,
                    StatusCodes.Status404NotFound,
                    "Return order not found",
                    $"Return order '{returnOrderId}' does not exist.");
            }

            var latestCondition = await db.QualityInspections
                .Where(x => x.ReturnOrderId == returnOrderId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => x.Condition)
                .FirstOrDefaultAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(latestCondition))
            {
                return Results.Ok(Array.Empty<HistoricalCaseDto>());
            }

            var payload = await db.HistoricalCaseViews
                .Where(x => x.Condition == latestCondition)
                .OrderBy(x => x.Outcome)
                .Select(x => new HistoricalCaseDto(x.Id, x.Condition, x.Outcome))
                .ToListAsync(cancellationToken);

            return Results.Ok(payload);
        });

        return endpoints;
    }
}
