namespace Wms.DomainService.Returns;

public sealed class ReturnOrder
{
    public ReturnOrder(Guid id, string returnNo)
    {
        Id = id;
        ReturnNo = returnNo;
        Status = "Open";
    }

    private ReturnOrder()
    {
    }

    public Guid Id { get; private set; }

    public string ReturnNo { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;

    public void MarkDisposed()
    {
        Status = "Disposed";
    }
}
