export interface AgUiEvent {
  type: string;
  traceId: string;
  workflowInstanceId: string;
  sessionId: string;
  payload: unknown;
}

export function subscribeToSopSession(
  sessionId: string,
  onEvent: (event: AgUiEvent) => void
) {
  const source = new EventSource(`/api/sop/sessions/${sessionId}/events`);
  const eventTypes = [
    "workflow.started",
    "message.delta",
    "tool.started",
    "tool.completed",
    "checkpoint.created",
    "approval.requested",
    "approval.completed",
    "citation.updated",
    "session.completed",
    "session.failed",
    "heartbeat"
  ];

  for (const eventType of eventTypes) {
    source.addEventListener(eventType, (evt) => {
      onEvent(JSON.parse((evt as MessageEvent).data) as AgUiEvent);
    });
  }

  return source;
}
