namespace Wms.DomainService.Commands;

public sealed class CommandDeduplication
{
    public CommandDeduplication(string idempotencyKey, string commandName)
    {
        IdempotencyKey = idempotencyKey;
        CommandName = commandName;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    private CommandDeduplication()
    {
    }

    public string IdempotencyKey { get; private set; } = string.Empty;

    public string CommandName { get; private set; } = string.Empty;

    public DateTimeOffset ProcessedAtUtc { get; private set; }
}
