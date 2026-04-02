namespace Wms.DomainService.Returns;

public sealed class HistoricalCaseView
{
    public Guid Id { get; private set; }

    public string Condition { get; private set; } = string.Empty;

    public string Outcome { get; private set; } = string.Empty;
}
