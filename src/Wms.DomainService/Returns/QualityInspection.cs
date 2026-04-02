namespace Wms.DomainService.Returns;

public sealed class QualityInspection
{
    public QualityInspection(Guid id, Guid returnOrderId, string condition, string notes = "")
    {
        Id = id;
        ReturnOrderId = returnOrderId;
        Condition = condition;
        Notes = notes;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private QualityInspection()
    {
    }

    public Guid Id { get; private set; }

    public Guid ReturnOrderId { get; private set; }

    public string Condition { get; private set; } = string.Empty;

    public string Notes { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
