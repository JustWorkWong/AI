using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Sop;
using Wms.DomainService.Persistence;

namespace Wms.DomainService.Endpoints;

public static class SopReadEndpoints
{
    public static IEndpointRouteBuilder MapSopReadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/internal/sop/candidates", async (
            string operationCode,
            string stepCode,
            HttpContext httpContext,
            WmsDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(operationCode) || string.IsNullOrWhiteSpace(stepCode))
            {
                return Program.CreateProblemResult(
                    httpContext,
                    StatusCodes.Status422UnprocessableEntity,
                    "Invalid SOP lookup",
                    "operationCode and stepCode are required.");
            }

            var payload = await (
                from document in db.SopDocuments
                join chunk in db.SopChunks on document.Id equals chunk.DocumentId
                where document.OperationCode == operationCode
                    && document.Status == "Published"
                    && chunk.StepCode == stepCode
                orderby document.DocumentCode
                select new SopCandidateDto(
                    document.Id,
                    document.DocumentCode,
                    document.Version,
                    document.Title))
                .Distinct()
                .ToListAsync(cancellationToken);

            return Results.Ok(payload);
        });

        endpoints.MapPost("/internal/sop/chunks/search", async (
            RetrieveSopChunksQuery query,
            HttpContext httpContext,
            WmsDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(query.OperationCode) || string.IsNullOrWhiteSpace(query.StepCode))
            {
                return Program.CreateProblemResult(
                    httpContext,
                    StatusCodes.Status422UnprocessableEntity,
                    "Invalid SOP lookup",
                    "operationCode and stepCode are required.");
            }

            if (query.CandidateDocumentIds.Count == 0)
            {
                return Results.Ok(Array.Empty<SopChunkDto>());
            }

            var payload = await (
                from chunk in db.SopChunks
                join document in db.SopDocuments on chunk.DocumentId equals document.Id
                where document.OperationCode == query.OperationCode
                    && query.CandidateDocumentIds.Contains(document.Id)
                    && chunk.StepCode == query.StepCode
                orderby document.DocumentCode, chunk.Sequence
                select new SopChunkDto(
                    chunk.Id,
                    document.Id,
                    document.DocumentCode,
                    document.Version,
                    chunk.StepCode,
                    chunk.Content))
                .ToListAsync(cancellationToken);

            return Results.Ok(payload);
        });

        return endpoints;
    }
}
