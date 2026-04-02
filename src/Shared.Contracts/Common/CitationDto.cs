namespace Shared.Contracts.Common;

public sealed record CitationDto(
    string SourceType,
    string SourceId,
    string Version,
    string Snippet);
