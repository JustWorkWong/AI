export interface CitationDto {
  sourceType: string;
  sourceId: string;
  version: string;
  snippet: string;
}

export interface ReturnOrderDto {
  returnOrderId: string;
  returnCode: string;
  qualityState: string;
  status: string;
  notes: string;
}

export interface DispositionSuggestionDto {
  returnOrderId: string;
  suggestedOutcome: string;
  riskLevel: string;
  citations: CitationDto[];
  approvalStatus: string;
}

export interface DispositionExecutionResultDto {
  workflowInstanceId: string;
  status: string;
  approvalReferenceId: string | null;
  outcome: string | null;
}

export interface ApprovalDecisionRequest {
  action: "Approve" | "Reject";
  actor: string;
}

export interface ToolInvocationDto {
  toolInvocationId: string;
  toolName: string;
  status: string;
  traceId: string;
  durationMs: number;
  inputSummary: string;
  outputSummary: string;
  errorMessage: string | null;
}

export interface WorkflowCheckpointDto {
  checkpointId: string;
  superstep: number;
  checkpointType: string;
  stateJson: string;
}

export interface DispositionExecutionTraceDto {
  workflowInstanceId: string;
  workflowCode: string;
  status: string;
  approvalReferenceId: string | null;
  toolInvocations: ToolInvocationDto[];
  checkpoints: WorkflowCheckpointDto[];
}

export interface ReturnWorkbenchViewDto {
  order: ReturnOrderDto;
  suggestion: DispositionSuggestionDto;
}

export interface SopExecutionViewDto {
  sessionId: string;
  operationCode: string;
  currentStepCode: string;
  citations: CitationDto[];
  requiresAcknowledgement: boolean;
}

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(`Request failed with ${response.status}`);
  }

  return (await response.json()) as T;
}

export async function getReturnWorkbench(returnOrderId: string): Promise<ReturnWorkbenchViewDto> {
  const response = await fetch(`/api/returns/workbench/${returnOrderId}`);
  return readJson<ReturnWorkbenchViewDto>(response);
}

export async function executeDisposition(
  returnOrderId: string,
  idempotencyKey: string
): Promise<DispositionExecutionResultDto> {
  const response = await fetch(`/api/returns/workbench/${returnOrderId}/execute`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ idempotencyKey })
  });

  return readJson<DispositionExecutionResultDto>(response);
}

export async function getDispositionTrace(
  workflowInstanceId: string
): Promise<DispositionExecutionTraceDto> {
  const response = await fetch(`/api/returns/workbench/executions/${workflowInstanceId}`);
  return readJson<DispositionExecutionTraceDto>(response);
}

export async function decideDispositionApproval(
  workflowInstanceId: string,
  request: ApprovalDecisionRequest
): Promise<DispositionExecutionResultDto> {
  const response = await fetch(`/api/returns/workbench/executions/${workflowInstanceId}/approval`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request)
  });

  return readJson<DispositionExecutionResultDto>(response);
}

export async function advanceSopStep(
  sessionId: string,
  stepCode: string,
  userInput: string
): Promise<SopExecutionViewDto> {
  const response = await fetch(`/api/sop/sessions/${sessionId}/steps`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ stepCode, userInput })
  });

  return readJson<SopExecutionViewDto>(response);
}
