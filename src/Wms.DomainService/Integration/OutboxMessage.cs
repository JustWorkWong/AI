namespace Wms.DomainService.Integration;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;

    public string Status { get; private set; } = "Pending";

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static OutboxMessage Create(string eventType, string payloadJson) =>
        new()
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            PayloadJson = payloadJson,
            Status = "Pending",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
}
