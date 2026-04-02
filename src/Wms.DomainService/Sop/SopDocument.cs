namespace Wms.DomainService.Sop;

public sealed class SopDocument
{
    public SopDocument(Guid id, string documentCode, string operationCode, string version, string title)
    {
        Id = id;
        DocumentCode = documentCode;
        OperationCode = operationCode;
        Version = version;
        Title = title;
        Status = "Published";
    }

    private SopDocument()
    {
    }

    public Guid Id { get; private set; }

    public string DocumentCode { get; private set; } = string.Empty;

    public string OperationCode { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Status { get; private set; } = string.Empty;
}
