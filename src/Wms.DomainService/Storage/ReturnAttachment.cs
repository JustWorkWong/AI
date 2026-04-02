namespace Wms.DomainService.Storage;

public sealed class ReturnAttachment
{
    public ReturnAttachment(Guid id, Guid returnOrderId, string objectKey, string contentType, string fileName)
    {
        Id = id;
        ReturnOrderId = returnOrderId;
        ObjectKey = objectKey;
        ContentType = contentType;
        FileName = fileName;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private ReturnAttachment()
    {
    }

    public Guid Id { get; private set; }

    public Guid ReturnOrderId { get; private set; }

    public string ObjectKey { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public string FileName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
