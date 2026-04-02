namespace Wms.DomainService.Integration;

public sealed class InboxMessage
{
    public InboxMessage(Guid id, string messageType)
    {
        Id = id;
        MessageType = messageType;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    private InboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public DateTimeOffset ProcessedAtUtc { get; private set; }
}
