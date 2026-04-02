using Wms.DomainService.Integration;
using Wms.DomainService.Persistence;
using Wms.DomainService.Storage;

namespace Wms.DomainService.Endpoints;

public static class AttachmentEndpoints
{
    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/internal/returns/{returnOrderId:guid}/attachments", async (
            Guid returnOrderId,
            HttpRequest request,
            IObjectStorage objectStorage,
            WmsDbContext db,
            CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.FirstOrDefault();

            if (file is null)
            {
                return Results.BadRequest("file is required");
            }

            await using var stream = file.OpenReadStream();

            var objectKey = $"returns/{returnOrderId}/{Guid.NewGuid():N}-{file.FileName}";
            await objectStorage.PutAsync(
                objectKey,
                stream,
                file.ContentType,
                cancellationToken);

            db.ReturnAttachments.Add(
                new ReturnAttachment(
                    Guid.NewGuid(),
                    returnOrderId,
                    objectKey,
                    file.ContentType,
                    file.FileName));

            var payload =
                $$"""{"returnOrderId":"{{returnOrderId}}","objectKey":"{{objectKey}}"}""";
            db.OutboxMessages.Add(
                OutboxMessage.Create("return-attachment-uploaded", payload));

            await db.SaveChangesAsync(cancellationToken);
            return Results.Accepted();
        });

        return endpoints;
    }
}
