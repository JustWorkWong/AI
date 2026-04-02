using Shared.Contracts.Common;

namespace Shared.Contracts.Sop;

public sealed record SopExecutionViewDto(
    Guid SessionId,
    string OperationCode,
    string CurrentStepCode,
    IReadOnlyList<CitationDto> Citations,
    bool RequiresAcknowledgement);

public sealed record SopCandidateDto(
    Guid DocumentId,
    string DocumentCode,
    string Version,
    string Title);

public sealed record SopChunkDto(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentCode,
    string Version,
    string StepCode,
    string Content);

public sealed record PublishSopCommand(
    string DocumentCode,
    string OperationCode,
    string Version,
    string Title,
    string RawContent);

public sealed record RetrieveSopQuery(string StepCode);

public sealed record RetrieveSopChunksQuery(
    string OperationCode,
    string StepCode,
    IReadOnlyList<Guid> CandidateDocumentIds);

public sealed record AdvanceSopStepRequest(string StepCode, string UserInput);
