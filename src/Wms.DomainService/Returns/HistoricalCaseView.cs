namespace Wms.DomainService.Returns;

public sealed class HistoricalCaseView
{
    public HistoricalCaseView(Guid id, string condition, string outcome)
    {
        Id = id;
        Condition = condition;
        Outcome = outcome;
    }

    private HistoricalCaseView()
    {
    }

    public Guid Id { get; private set; }

    public string Condition { get; private set; } = string.Empty;

    public string Outcome { get; private set; } = string.Empty;
}
