import { describe, expect, it } from "vitest";
import { createReturnWorkbench } from "./useReturnWorkbench";
import type {
  ApprovalDecisionRequest,
  DispositionExecutionResultDto,
  DispositionExecutionTraceDto,
  ReturnWorkbenchViewDto
} from "../lib/api";

const RETURN_ORDER_ID = "11111111-1111-1111-1111-111111111111";
const WORKFLOW_INSTANCE_ID = "22222222-2222-2222-2222-222222222222";
const APPROVAL_REFERENCE_ID = "33333333-3333-3333-3333-333333333333";

describe("createReturnWorkbench", () => {
  it("should mark approval pending after execution and complete after approval", async () => {
    const workbench = createReturnWorkbench(
      createApiStub({
        executeResult: createExecutionResult({ status: "WaitingForApproval", outcome: null }),
        traceResult: createTraceResult({ status: "WaitingApproval" }),
        approvalResult: createExecutionResult({ status: "Completed", outcome: "Scrap" })
      })
    );

    await workbench.load(RETURN_ORDER_ID);
    await workbench.execute(RETURN_ORDER_ID, "idem-001");

    expect(workbench.canApprove.value).toBe(true);
    expect(workbench.suggestion.value?.approvalStatus).toBe("Pending");
    expect(workbench.order.value?.status).toBe("PendingInspection");

    await workbench.decideApproval("Approve", "manager-web");

    expect(workbench.canApprove.value).toBe(false);
    expect(workbench.executionResult.value?.status).toBe("Completed");
    expect(workbench.executionResult.value?.outcome).toBe("Scrap");
    expect(workbench.suggestion.value?.approvalStatus).toBe("Completed");
    expect(workbench.order.value?.status).toBe("Disposed");
  });

  it("should keep order open and mark suggestion rejected after approval rejection", async () => {
    const workbench = createReturnWorkbench(
      createApiStub({
        executeResult: createExecutionResult({ status: "WaitingForApproval", outcome: null }),
        traceResult: createTraceResult({ status: "Rejected" }),
        approvalResult: createExecutionResult({ status: "Rejected", outcome: null })
      })
    );

    await workbench.load(RETURN_ORDER_ID);
    await workbench.execute(RETURN_ORDER_ID, "idem-002");
    await workbench.decideApproval("Reject", "manager-web");

    expect(workbench.canApprove.value).toBe(false);
    expect(workbench.executionResult.value?.status).toBe("Rejected");
    expect(workbench.suggestion.value?.approvalStatus).toBe("Rejected");
    expect(workbench.order.value?.status).toBe("PendingInspection");
  });

  it("should restore approval flags and surface error message when approval fails", async () => {
    const workbench = createReturnWorkbench(
      createApiStub({
        executeResult: createExecutionResult({ status: "WaitingForApproval", outcome: null }),
        traceResult: createTraceResult({ status: "WaitingApproval" }),
        decideError: new Error("approval failed")
      })
    );

    await workbench.load(RETURN_ORDER_ID);
    await workbench.execute(RETURN_ORDER_ID, "idem-003");
    await workbench.decideApproval("Approve", "manager-web");

    expect(workbench.isApproving.value).toBe(false);
    expect(workbench.canApprove.value).toBe(true);
    expect(workbench.executionResult.value?.status).toBe("WaitingForApproval");
    expect(workbench.errorMessage.value).toBe("approval failed");
  });

  it("should surface error message and avoid throwing when initial load fails", async () => {
    const workbench = createReturnWorkbench({
      ...createApiStub(),
      getReturnWorkbench: async () => {
        throw new Error("load failed");
      }
    });

    await workbench.load(RETURN_ORDER_ID);

    expect(workbench.order.value).toBeNull();
    expect(workbench.suggestion.value).toBeNull();
    expect(workbench.errorMessage.value).toBe("load failed");
  });
});

function createApiStub(options?: {
  workbenchView?: ReturnWorkbenchViewDto;
  executeResult?: DispositionExecutionResultDto;
  traceResult?: DispositionExecutionTraceDto;
  approvalResult?: DispositionExecutionResultDto;
  decideError?: Error;
}) {
  const workbenchView = options?.workbenchView ?? createWorkbenchView();
  const executeResult = options?.executeResult ?? createExecutionResult({ status: "WaitingForApproval", outcome: null });
  const traceResult = options?.traceResult ?? createTraceResult({ status: "WaitingApproval" });
  const approvalResult = options?.approvalResult ?? createExecutionResult({ status: "Completed", outcome: "Scrap" });

  return {
    getReturnWorkbench: async () => workbenchView,
    executeDisposition: async () => executeResult,
    getDispositionTrace: async () => traceResult,
    decideDispositionApproval: async (_workflowInstanceId: string, _request: ApprovalDecisionRequest) => {
      if (options?.decideError) {
        throw options.decideError;
      }

      return approvalResult;
    }
  };
}

function createWorkbenchView(): ReturnWorkbenchViewDto {
  return {
    order: {
      returnOrderId: RETURN_ORDER_ID,
      returnCode: "RMA-001",
      qualityState: "Broken",
      status: "PendingInspection",
      notes: "Damaged shell"
    },
    suggestion: {
      returnOrderId: RETURN_ORDER_ID,
      suggestedOutcome: "Scrap",
      riskLevel: "High",
      citations: [],
      approvalStatus: "Pending"
    }
  };
}

function createExecutionResult(overrides: {
  status: DispositionExecutionResultDto["status"];
  outcome: DispositionExecutionResultDto["outcome"];
}): DispositionExecutionResultDto {
  return {
    workflowInstanceId: WORKFLOW_INSTANCE_ID,
    status: overrides.status,
    approvalReferenceId: APPROVAL_REFERENCE_ID,
    outcome: overrides.outcome
  };
}

function createTraceResult(overrides: {
  status: DispositionExecutionTraceDto["status"];
}): DispositionExecutionTraceDto {
  return {
    workflowInstanceId: WORKFLOW_INSTANCE_ID,
    workflowCode: "return-disposition-execute",
    status: overrides.status,
    approvalReferenceId: APPROVAL_REFERENCE_ID,
    toolInvocations: [],
    checkpoints: []
  };
}
