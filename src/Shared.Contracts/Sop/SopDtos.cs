using Shared.Contracts.Common;

namespace Shared.Contracts.Sop;

public sealed record SopExecutionViewDto(
    Guid SessionId,
    string OperationCode,
    string CurrentStepCode,
    IReadOnlyList<CitationDto> Citations,
    bool RequiresAcknowledgement);

public sealed record PublishSopCommand(string RawContent);

public sealed record RetrieveSopQuery(string StepCode);

public sealed record AdvanceSopStepRequest(string StepCode, string UserInput);
