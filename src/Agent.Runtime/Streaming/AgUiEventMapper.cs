using Shared.Contracts.Realtime;

namespace Agent.Runtime.Streaming;

public static class AgUiEventMapper
{
    public static AgUiEvent MapToolStarted(
        string toolName,
        string traceId,
        Guid workflowInstanceId,
        Guid sessionId)
    {
        return new AgUiEvent(
            "tool.started",
            traceId,
            workflowInstanceId,
            sessionId,
            new { tool_name = toolName });
    }

    public static AgUiEvent MapHeartbeat(Guid sessionId)
    {
        return new AgUiEvent(
            "heartbeat",
            "trace-demo",
            Guid.Empty,
            sessionId,
            new { status = "alive" });
    }
}
