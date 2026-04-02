using Shared.Contracts.Sop;
using Wms.DomainService.Persistence;
using Wms.DomainService.Sop;

namespace Wms.DomainService.Endpoints;

public static class SopPublishEndpoints
{
    public static IEndpointRouteBuilder MapSopPublishEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/internal/sop/publish", async (
            PublishSopCommand command,
            WmsDbContext db,
            CancellationToken cancellationToken) =>
        {
            var chunks = SopChunker.Split(command.RawContent);
            if (chunks.Count == 0)
            {
                return Results.BadRequest(new { error = "SOP content did not contain any STEP blocks." });
            }

            var documentId = Guid.NewGuid();
            db.SopDocuments.Add(new SopDocument(
                documentId,
                command.DocumentCode,
                command.OperationCode,
                command.Version,
                command.Title));

            foreach (var chunk in chunks)
            {
                db.SopChunks.Add(new SopChunk(
                    Guid.NewGuid(),
                    documentId,
                    chunk.StepCode,
                    chunk.Sequence,
                    chunk.Content));
            }

            await db.SaveChangesAsync(cancellationToken);
            return Results.Accepted($"/internal/sop/documents/{documentId}");
        });

        return endpoints;
    }
}
