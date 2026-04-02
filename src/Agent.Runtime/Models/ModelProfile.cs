namespace Agent.Runtime.Models;

public sealed class ModelProfile
{
    public string ProfileCode { get; init; } = string.Empty;

    public string Provider { get; init; } = string.Empty;

    public string Endpoint { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public bool SupportsTools { get; init; }

    public bool SupportsStreaming { get; init; }
}
