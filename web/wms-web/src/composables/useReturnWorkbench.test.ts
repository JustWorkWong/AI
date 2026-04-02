import { describe, expect, it } from "vitest";
import { createReturnWorkbench } from "./useReturnWorkbench";

describe("createReturnWorkbench", () => {
  it("should mark approval pending after execution and complete after approval", async () => {
    const returnOrderId = "11111111-1111-1111-1111-111111111111";
    const workflowInstanceId = "22222222-2222-2222-2222-222222222222";

    const workbench = createReturnWorkbench({
      getReturnWorkbench: async () => ({
        order: {
          returnOrderId,
          returnCode: "RMA-001",
          qualityState: "Broken",
          status: "PendingInspection",
          notes: "Damaged shell"
        },
        suggestion: {
          returnOrderId,
          suggestedOutcome: "Scrap",
          riskLevel: "High",
          citations: [],
          approvalStatus: "Pending"
        }
      }),
      executeDisposition: async () => ({
        workflowInstanceId,
        status: "WaitingForApproval",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        outcome: null
      }),
      getDispositionTrace: async () => ({
        workflowInstanceId,
        workflowCode: "return-disposition-execute",
        status: "WaitingApproval",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        toolInvocations: [],
        checkpoints: []
      }),
      decideDispositionApproval: async () => ({
        workflowInstanceId,
        status: "Completed",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        outcome: "Scrap"
      })
    });

    await workbench.load(returnOrderId);
    await workbench.execute(returnOrderId, "idem-001");

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
    const returnOrderId = "11111111-1111-1111-1111-111111111111";
    const workflowInstanceId = "22222222-2222-2222-2222-222222222222";

    const workbench = createReturnWorkbench({
      getReturnWorkbench: async () => ({
        order: {
          returnOrderId,
          returnCode: "RMA-001",
          qualityState: "Broken",
          status: "PendingInspection",
          notes: "Damaged shell"
        },
        suggestion: {
          returnOrderId,
          suggestedOutcome: "Scrap",
          riskLevel: "High",
          citations: [],
          approvalStatus: "Pending"
        }
      }),
      executeDisposition: async () => ({
        workflowInstanceId,
        status: "WaitingForApproval",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        outcome: null
      }),
      getDispositionTrace: async () => ({
        workflowInstanceId,
        workflowCode: "return-disposition-execute",
        status: "Rejected",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        toolInvocations: [],
        checkpoints: []
      }),
      decideDispositionApproval: async () => ({
        workflowInstanceId,
        status: "Rejected",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        outcome: null
      })
    });

    await workbench.load(returnOrderId);
    await workbench.execute(returnOrderId, "idem-002");
    await workbench.decideApproval("Reject", "manager-web");

    expect(workbench.canApprove.value).toBe(false);
    expect(workbench.executionResult.value?.status).toBe("Rejected");
    expect(workbench.suggestion.value?.approvalStatus).toBe("Rejected");
    expect(workbench.order.value?.status).toBe("PendingInspection");
  });

  it("should restore approval flags and surface error message when approval fails", async () => {
    const returnOrderId = "11111111-1111-1111-1111-111111111111";
    const workflowInstanceId = "22222222-2222-2222-2222-222222222222";

    const workbench = createReturnWorkbench({
      getReturnWorkbench: async () => ({
        order: {
          returnOrderId,
          returnCode: "RMA-001",
          qualityState: "Broken",
          status: "PendingInspection",
          notes: "Damaged shell"
        },
        suggestion: {
          returnOrderId,
          suggestedOutcome: "Scrap",
          riskLevel: "High",
          citations: [],
          approvalStatus: "Pending"
        }
      }),
      executeDisposition: async () => ({
        workflowInstanceId,
        status: "WaitingForApproval",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        outcome: null
      }),
      getDispositionTrace: async () => ({
        workflowInstanceId,
        workflowCode: "return-disposition-execute",
        status: "WaitingApproval",
        approvalReferenceId: "33333333-3333-3333-3333-333333333333",
        toolInvocations: [],
        checkpoints: []
      }),
      decideDispositionApproval: async () => {
        throw new Error("approval failed");
      }
    });

    await workbench.load(returnOrderId);
    await workbench.execute(returnOrderId, "idem-003");
    await workbench.decideApproval("Approve", "manager-web");

    expect(workbench.isApproving.value).toBe(false);
    expect(workbench.canApprove.value).toBe(true);
    expect(workbench.executionResult.value?.status).toBe("WaitingForApproval");
    expect(workbench.errorMessage.value).toBe("approval failed");
  });
});
