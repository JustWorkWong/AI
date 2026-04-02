namespace Wms.DomainService.Sop;

public sealed class SopChunk
{
    public SopChunk(Guid id, Guid documentId, string stepCode, int sequence, string content)
    {
        Id = id;
        DocumentId = documentId;
        StepCode = stepCode;
        Sequence = sequence;
        Content = content;
    }

    private SopChunk()
    {
    }

    public Guid Id { get; private set; }

    public Guid DocumentId { get; private set; }

    public string StepCode { get; private set; } = string.Empty;

    public int Sequence { get; private set; }

    public string Content { get; private set; } = string.Empty;
}
