namespace Wms.DomainService.Returns;

public sealed class DispositionDecision
{
    public DispositionDecision(Guid id, Guid returnOrderId, string outcome)
    {
        Id = id;
        ReturnOrderId = returnOrderId;
        Outcome = outcome;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private DispositionDecision()
    {
    }

    public Guid Id { get; private set; }

    public Guid ReturnOrderId { get; private set; }

    public string Outcome { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
